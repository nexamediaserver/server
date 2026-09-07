package app

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"

	"nexa/internal/bootstrap"
	"nexa/internal/database/sqlite"
)

type App struct {
	DataDir string
	db      *sqlite.Database
	state   bootstrap.StateStore
}

func NewApp(dataDir string) (*App, error) {
	cfg, err := bootstrap.Ensure(dataDir)
	if err != nil {
		return nil, fmt.Errorf("prepare data directory: %w", err)
	}

	db, err := sqlite.Open(cfg.Root)
	if err != nil {
		return nil, fmt.Errorf("open database: %w", err)
	}
	if err := db.Migrate(context.Background()); err != nil {
		_ = db.Close()
		return nil, fmt.Errorf("migrate database: %w", err)
	}

	return &App{
		DataDir: cfg.Root,
		db:      db,
		state:   sqlite.NewStateStore(db),
	}, nil
}

func (a *App) Close() error {
	return a.db.Close()
}

func (a *App) healthzHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	_ = json.NewEncoder(w).Encode(map[string]string{"status": "ok"})
}

func (a *App) setupStatusHandler(w http.ResponseWriter, r *http.Request) {
	state, err := a.state.Read()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	_ = json.NewEncoder(w).Encode(map[string]string{"setup_state": string(state)})
}

func (a *App) setupStartHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	state, err := a.state.Read()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	if state != bootstrap.StateUninitialized {
		http.Error(w, "setup has already started", http.StatusConflict)
		return
	}
	if err := a.state.Write(bootstrap.StateSetupInProgress); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	writeSetupState(w, bootstrap.StateSetupInProgress)
}

func (a *App) setupCompleteHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	state, err := a.state.Read()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	if state != bootstrap.StateSetupInProgress {
		http.Error(w, "setup is not in progress", http.StatusConflict)
		return
	}
	if err := a.state.Write(bootstrap.StateSetupComplete); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	writeSetupState(w, bootstrap.StateSetupComplete)
}

func writeSetupState(w http.ResponseWriter, state bootstrap.State) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	_ = json.NewEncoder(w).Encode(map[string]string{"setup_state": string(state)})
}

func (a *App) NewMux() *http.ServeMux {
	mux := http.NewServeMux()
	mux.HandleFunc("/healthz", a.healthzHandler)
	mux.HandleFunc("/api/v1/setup/status", a.setupStatusHandler)
	mux.HandleFunc("/api/v1/setup/start", a.setupStartHandler)
	mux.HandleFunc("/api/v1/setup/complete", a.setupCompleteHandler)
	return mux
}

func defaultApp() *App {
	dataDir := bootstrap.DefaultDataDir()
	app, err := NewApp(dataDir)
	if err != nil {
		panic(err)
	}
	return app
}

func NewMux() *http.ServeMux {
	return defaultApp().NewMux()
}
