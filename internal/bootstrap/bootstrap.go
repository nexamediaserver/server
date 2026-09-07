package bootstrap

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
)

const (
	StateUninitialized   State = "uninitialized"
	StateSetupInProgress State = "setup_in_progress"
	StateSetupComplete   State = "setup_complete"
)

type State string

type DataDir struct {
	Root              string
	ConfigDir         string
	BlobsDir          string
	CacheDir          string
	ExtensionsDir     string
	ExtensionStateDir string
}

type FileStateStore struct {
	path string
}

type persistedState struct {
	SetupState string `json:"setup_state"`
}

func DefaultDataDir() string {
	if dir := os.Getenv("NEXA_DATA_DIR"); dir != "" {
		return dir
	}
	return "./data"
}

func Ensure(root string) (DataDir, error) {
	if root == "" {
		return DataDir{}, errors.New("data directory cannot be empty")
	}

	absRoot, err := filepath.Abs(root)
	if err != nil {
		return DataDir{}, fmt.Errorf("resolve data dir: %w", err)
	}

	cfg := DataDir{
		Root:              absRoot,
		ConfigDir:         filepath.Join(absRoot, "config"),
		BlobsDir:          filepath.Join(absRoot, "blobs"),
		CacheDir:          filepath.Join(absRoot, "cache"),
		ExtensionsDir:     filepath.Join(absRoot, "extensions"),
		ExtensionStateDir: filepath.Join(absRoot, "extension-state"),
	}

	for _, dir := range []string{
		cfg.Root,
		cfg.ConfigDir,
		cfg.BlobsDir,
		cfg.CacheDir,
		cfg.ExtensionsDir,
		cfg.ExtensionStateDir,
	} {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			return DataDir{}, fmt.Errorf("create directory %s: %w", dir, err)
		}
	}

	return cfg, nil
}

func NewFileStateStore(dataDir string) *FileStateStore {
	return &FileStateStore{path: filepath.Join(dataDir, "setup.json")}
}

func (s *FileStateStore) Read() (State, error) {
	file, err := os.ReadFile(s.path)
	if err != nil {
		if errors.Is(err, os.ErrNotExist) {
			return StateUninitialized, nil
		}
		return "", err
	}

	var value persistedState
	if err := json.Unmarshal(file, &value); err != nil {
		return "", fmt.Errorf("decode setup state: %w", err)
	}

	if value.SetupState == "" {
		return StateUninitialized, nil
	}

	return State(value.SetupState), nil
}

func (s *FileStateStore) Write(state State) error {
	payload, err := json.MarshalIndent(persistedState{SetupState: string(state)}, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal setup state: %w", err)
	}

	if err := os.WriteFile(s.path, payload, 0o600); err != nil {
		return fmt.Errorf("write setup state: %w", err)
	}

	return nil
}
