package sqlite

import (
	"context"
	"fmt"

	"nexa/internal/database/sqlite/sqlcgen"
)

const (
	defaultItemPageSize = 50
	maxItemPageSize     = 500
)

// Item is a record belonging to a library. Domain-specific values live in
// field values, not on Item itself.
type Item struct {
	ID        string
	LibraryID string
	Status    string
	Revision  int
	CreatedAt string
	UpdatedAt string
}

func itemFromRow(row sqlcgen.Item) Item {
	return Item{
		ID:        row.ID,
		LibraryID: row.LibraryID,
		Status:    row.Status,
		Revision:  int(row.Revision),
		CreatedAt: row.CreatedAt,
		UpdatedAt: row.UpdatedAt,
	}
}

// ItemCursor identifies a position in a library's item listing for keyset
// pagination. It is opaque to callers and should be round-tripped from the
// NextCursor of a previous page.
type ItemCursor struct {
	CreatedAt string
	ID        string
}

// CreateItem creates a new item in a library with revision 1 and returns its
// generated ID. The insert runs on the database's write queue so concurrent
// callers serialize safely instead of contending for SQLite's write lock.
func (s *DomainStore) CreateItem(ctx context.Context, libraryID, status string) (string, error) {
	if status == "" {
		status = "active"
	}
	id := newID()
	now := nowTimestamp()
	err := s.writes.submit(ctx, func(ctx context.Context) error {
		return s.queries.CreateItem(ctx, sqlcgen.CreateItemParams{
			ID:        id,
			LibraryID: libraryID,
			Status:    status,
			Revision:  1,
			CreatedAt: now,
			UpdatedAt: now,
		})
	})
	if err != nil {
		return "", fmt.Errorf("create item in library %q: %w", libraryID, err)
	}
	return id, nil
}

func (s *DomainStore) GetItem(ctx context.Context, id string) (Item, error) {
	row, err := s.queries.GetItem(ctx, id)
	if err != nil {
		return Item{}, fmt.Errorf("get item %q: %w", id, err)
	}
	return itemFromRow(row), nil
}

// ListItemsForLibrary returns up to limit items for a library ordered by
// (created_at, id), starting strictly after cursor (or from the start when
// cursor is nil). It uses keyset pagination rather than OFFSET/LIMIT so that
// listing performance does not degrade on libraries with hundreds of
// thousands to millions of items. The returned cursor is non-nil only when
// there is a further page to fetch.
func (s *DomainStore) ListItemsForLibrary(ctx context.Context, libraryID string, limit int, cursor *ItemCursor) ([]Item, *ItemCursor, error) {
	boundedLimit := boundLimit(limit, defaultItemPageSize, maxItemPageSize)

	// Fetch one extra row to determine whether a next page exists without
	// a separate count query.
	fetchLimit := boundedLimit + 1

	var rows []sqlcgen.Item
	var err error
	if cursor == nil {
		rows, err = s.queries.ListItemsForLibraryFirstPage(ctx, sqlcgen.ListItemsForLibraryFirstPageParams{
			LibraryID: libraryID,
			Limit:     fetchLimit,
		})
	} else {
		rows, err = s.queries.ListItemsForLibraryAfterCursor(ctx, sqlcgen.ListItemsForLibraryAfterCursorParams{
			LibraryID:   libraryID,
			CreatedAt:   cursor.CreatedAt,
			CreatedAt_2: cursor.CreatedAt,
			ID:          cursor.ID,
			Limit:       fetchLimit,
		})
	}
	if err != nil {
		return nil, nil, fmt.Errorf("list items for library %q: %w", libraryID, err)
	}

	var next *ItemCursor
	if int64(len(rows)) > boundedLimit {
		rows = rows[:boundedLimit]
		last := rows[len(rows)-1]
		next = &ItemCursor{CreatedAt: last.CreatedAt, ID: last.ID}
	}

	items := make([]Item, 0, len(rows))
	for _, row := range rows {
		items = append(items, itemFromRow(row))
	}
	return items, next, nil
}
