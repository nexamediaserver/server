package bootstrap

import (
	"os"
	"path/filepath"
	"testing"
)

func TestEnsureCreatesExpectedDirectories(t *testing.T) {
	root := filepath.Join(t.TempDir(), "nexa-data")

	cfg, err := Ensure(root)
	if err != nil {
		t.Fatalf("Ensure returned error: %v", err)
	}

	for _, dir := range []string{
		cfg.Root,
		cfg.ConfigDir,
		cfg.BlobsDir,
		cfg.CacheDir,
		cfg.ExtensionsDir,
		cfg.ExtensionStateDir,
	} {
		if _, err := os.Stat(dir); err != nil {
			t.Fatalf("expected directory %s to exist: %v", dir, err)
		}
	}
}

func TestStateStoreRoundTrip(t *testing.T) {
	root := filepath.Join(t.TempDir(), "nexa-data")
	if _, err := Ensure(root); err != nil {
		t.Fatalf("Ensure returned error: %v", err)
	}

	store := NewFileStateStore(root)

	if state, err := store.Read(); err != nil || state != StateUninitialized {
		t.Fatalf("expected uninitialized state, got state=%q err=%v", state, err)
	}

	if err := store.Write(StateSetupInProgress); err != nil {
		t.Fatalf("Write returned error: %v", err)
	}

	state, err := store.Read()
	if err != nil {
		t.Fatalf("Read returned error after write: %v", err)
	}
	if state != StateSetupInProgress {
		t.Fatalf("expected setup_in_progress state, got %q", state)
	}
}
