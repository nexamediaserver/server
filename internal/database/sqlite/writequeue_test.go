package sqlite

import (
	"context"
	"sync"
	"testing"
)

// TestCreateItemConcurrentWriters proves that many concurrent callers can
// create items at once through the write queue without hitting SQLITE_BUSY:
// the queue serializes the underlying writes so goroutines wait on a Go
// channel instead of racing SQLite's own write lock.
func TestCreateItemConcurrentWriters(t *testing.T) {
	dataDir := t.TempDir()
	db, err := Open(dataDir)
	if err != nil {
		t.Fatalf("Open returned error: %v", err)
	}
	defer func() { _ = db.Close() }()

	if err := db.Migrate(context.Background()); err != nil {
		t.Fatalf("Migrate returned error: %v", err)
	}

	ctx := context.Background()
	store := NewDomainStore(db)

	libraryID, err := store.CreateLibrary(ctx, "movies", "Movies", "movies")
	if err != nil {
		t.Fatalf("CreateLibrary returned error: %v", err)
	}

	const writers = 50
	var wg sync.WaitGroup
	errs := make([]error, writers)
	ids := make([]string, writers)
	wg.Add(writers)
	for i := 0; i < writers; i++ {
		go func(i int) {
			defer wg.Done()
			ids[i], errs[i] = store.CreateItem(ctx, libraryID, "")
		}(i)
	}
	wg.Wait()

	for i, err := range errs {
		if err != nil {
			t.Fatalf("CreateItem[%d] returned error: %v", i, err)
		}
	}

	seen := make(map[string]bool, writers)
	for _, id := range ids {
		if id == "" {
			t.Fatalf("expected non-empty item id")
		}
		if seen[id] {
			t.Fatalf("duplicate item id %q returned to two callers", id)
		}
		seen[id] = true
	}

	var count int
	if err := db.QueryRow("SELECT COUNT(*) FROM items WHERE library_id = ?", libraryID).Scan(&count); err != nil {
		t.Fatalf("count items: %v", err)
	}
	if count != writers {
		t.Fatalf("expected %d rows created, got %d", writers, count)
	}
}
