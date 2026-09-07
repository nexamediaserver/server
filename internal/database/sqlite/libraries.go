package sqlite

import (
	"context"
	"fmt"

	"nexa/internal/database/sqlite/sqlcgen"
)

const (
	defaultLibraryPageSize = 50
	maxLibraryPageSize     = 200
)

// Library is a configured content domain (e.g. Movies, Music, Books).
type Library struct {
	ID          string
	Slug        string
	Name        string
	ContentType string
	CreatedAt   string
	UpdatedAt   string
}

func libraryFromRow(row sqlcgen.Library) Library {
	return Library{
		ID:          row.ID,
		Slug:        row.Slug,
		Name:        row.Name,
		ContentType: row.ContentType,
		CreatedAt:   row.CreatedAt,
		UpdatedAt:   row.UpdatedAt,
	}
}

// CreateLibrary creates a new library and returns its generated ID. The
// insert runs on the database's write queue so concurrent callers serialize
// safely instead of contending for SQLite's write lock.
func (s *DomainStore) CreateLibrary(ctx context.Context, slug, name, contentType string) (string, error) {
	id := newID()
	now := nowTimestamp()
	err := s.writes.submit(ctx, func(ctx context.Context) error {
		return s.queries.CreateLibrary(ctx, sqlcgen.CreateLibraryParams{
			ID:          id,
			Slug:        slug,
			Name:        name,
			ContentType: contentType,
			CreatedAt:   now,
			UpdatedAt:   now,
		})
	})
	if err != nil {
		return "", fmt.Errorf("create library: %w", err)
	}
	return id, nil
}

func (s *DomainStore) GetLibrary(ctx context.Context, id string) (Library, error) {
	row, err := s.queries.GetLibrary(ctx, id)
	if err != nil {
		return Library{}, fmt.Errorf("get library %q: %w", id, err)
	}
	return libraryFromRow(row), nil
}

// ListLibraries returns libraries ordered by name, paginated with a bounded
// OFFSET/LIMIT. Libraries are an admin-configured, low-cardinality collection
// (one per content domain), unlike items, so simple offset pagination is
// acceptable here and avoids a cursor-encoding scheme with no real benefit.
func (s *DomainStore) ListLibraries(ctx context.Context, limit, offset int) ([]Library, error) {
	if offset < 0 {
		offset = 0
	}
	rows, err := s.queries.ListLibraries(ctx, sqlcgen.ListLibrariesParams{
		Limit:  boundLimit(limit, defaultLibraryPageSize, maxLibraryPageSize),
		Offset: int64(offset),
	})
	if err != nil {
		return nil, fmt.Errorf("list libraries: %w", err)
	}
	libraries := make([]Library, 0, len(rows))
	for _, row := range rows {
		libraries = append(libraries, libraryFromRow(row))
	}
	return libraries, nil
}
