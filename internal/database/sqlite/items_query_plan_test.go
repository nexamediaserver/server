package sqlite

import (
	"context"
	"strings"
	"testing"
)

// TestListItemsForLibraryUsesKeysetIndex confirms both the first-page and
// after-cursor queries behind ListItemsForLibrary are answered via
// idx_items_library_created_id rather than a full table scan or a separate
// sort step, which is what keeps keyset pagination cheap on libraries with
// hundreds of thousands to millions of items.
func TestListItemsForLibraryUsesKeysetIndex(t *testing.T) {
	dataDir := t.TempDir()
	db, err := Open(dataDir)
	if err != nil {
		t.Fatalf("Open returned error: %v", err)
	}
	defer func() { _ = db.Close() }()

	if err := db.Migrate(context.Background()); err != nil {
		t.Fatalf("Migrate returned error: %v", err)
	}

	plans := []struct {
		name  string
		query string
		args  []any
	}{
		{
			name: "first page",
			query: `SELECT id, library_id, status, revision, created_at, updated_at
				FROM items WHERE library_id = ? ORDER BY created_at, id LIMIT ?`,
			args: []any{"lib1", 10},
		},
		{
			name: "after cursor",
			query: `SELECT id, library_id, status, revision, created_at, updated_at
				FROM items WHERE library_id = ?
				AND (created_at > ? OR (created_at = ? AND id > ?))
				ORDER BY created_at, id LIMIT ?`,
			args: []any{"lib1", "2020", "2020", "x", 10},
		},
	}

	for _, p := range plans {
		t.Run(p.name, func(t *testing.T) {
			rows, err := db.Query("EXPLAIN QUERY PLAN "+p.query, p.args...)
			if err != nil {
				t.Fatalf("EXPLAIN QUERY PLAN: %v", err)
			}
			defer func() { _ = rows.Close() }()

			var plan strings.Builder
			for rows.Next() {
				var id, parent, notUsed int
				var detail string
				if err := rows.Scan(&id, &parent, &notUsed, &detail); err != nil {
					t.Fatalf("scan query plan row: %v", err)
				}
				plan.WriteString(detail)
				plan.WriteString("\n")
			}
			if err := rows.Err(); err != nil {
				t.Fatalf("iterate query plan rows: %v", err)
			}

			got := plan.String()
			if !strings.Contains(got, "idx_items_library_created_id") {
				t.Fatalf("expected query plan to use idx_items_library_created_id, got:\n%s", got)
			}
			if strings.Contains(got, "SCAN") && !strings.Contains(got, "SEARCH") {
				t.Fatalf("expected an index search, got a table scan:\n%s", got)
			}
			if strings.Contains(got, "TEMP B-TREE") {
				t.Fatalf("expected no extra sort step, got:\n%s", got)
			}
		})
	}
}
