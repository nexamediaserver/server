package sqlite

import (
	"context"
	"path/filepath"
	"testing"

	"nexa/internal/bootstrap"
)

func TestOpenMigrateAndStateRoundTrip(t *testing.T) {
	dataDir := t.TempDir()
	db, err := Open(dataDir)
	if err != nil {
		t.Fatalf("Open returned error: %v", err)
	}
	defer func() { _ = db.Close() }()

	if err := db.Migrate(context.Background()); err != nil {
		t.Fatalf("Migrate returned error: %v", err)
	}

	var migrationCount int
	if err := db.QueryRow("SELECT COUNT(*) FROM schema_migrations").Scan(&migrationCount); err != nil {
		t.Fatalf("read migration count: %v", err)
	}
	if migrationCount != 1 {
		t.Fatalf("expected one applied migration, got %d", migrationCount)
	}

	if err := db.Migrate(context.Background()); err != nil {
		t.Fatalf("second Migrate returned error: %v", err)
	}

	store := NewStateStore(db)
	state, err := store.Read()
	if err != nil {
		t.Fatalf("Read returned error: %v", err)
	}
	if state != bootstrap.StateUninitialized {
		t.Fatalf("expected uninitialized state, got %q", state)
	}

	if err := store.Write(bootstrap.StateSetupInProgress); err != nil {
		t.Fatalf("Write returned error: %v", err)
	}
	state, err = store.Read()
	if err != nil {
		t.Fatalf("Read after write returned error: %v", err)
	}
	if state != bootstrap.StateSetupInProgress {
		t.Fatalf("expected setup_in_progress, got %q", state)
	}

	if _, err := filepath.Abs(dataDir); err != nil {
		t.Fatalf("unexpected data directory error: %v", err)
	}
}
