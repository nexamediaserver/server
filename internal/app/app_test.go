package app

import (
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"strings"
	"testing"
)

func TestHealthz(t *testing.T) {
	instance, err := NewApp(filepath.Join(t.TempDir(), "data"))
	if err != nil {
		t.Fatalf("NewApp returned error: %v", err)
	}
	defer func() { _ = instance.Close() }()

	req := httptest.NewRequest(http.MethodGet, "/healthz", nil)
	res := httptest.NewRecorder()

	instance.healthzHandler(res, req)

	if res.Code != http.StatusOK {
		t.Fatalf("expected status %d, got %d", http.StatusOK, res.Code)
	}

	if !strings.Contains(res.Body.String(), "ok") {
		t.Fatalf("expected ok payload, got %q", res.Body.String())
	}
}

func TestSetupStatus(t *testing.T) {
	instance, err := NewApp(filepath.Join(t.TempDir(), "data"))
	if err != nil {
		t.Fatalf("NewApp returned error: %v", err)
	}
	defer func() { _ = instance.Close() }()

	req := httptest.NewRequest(http.MethodGet, "/api/v1/setup/status", nil)
	res := httptest.NewRecorder()

	instance.setupStatusHandler(res, req)

	if res.Code != http.StatusOK {
		t.Fatalf("expected status %d, got %d", http.StatusOK, res.Code)
	}

	if !strings.Contains(res.Body.String(), "uninitialized") {
		t.Fatalf("expected uninitialized payload, got %q", res.Body.String())
	}
}

func TestSetupTransitionsPersistThroughHTTP(t *testing.T) {
	instance, err := NewApp(filepath.Join(t.TempDir(), "data"))
	if err != nil {
		t.Fatalf("NewApp returned error: %v", err)
	}
	defer func() { _ = instance.Close() }()

	startReq := httptest.NewRequest(http.MethodPost, "/api/v1/setup/start", nil)
	startRes := httptest.NewRecorder()
	instance.NewMux().ServeHTTP(startRes, startReq)
	if startRes.Code != http.StatusOK {
		t.Fatalf("expected setup start status %d, got %d", http.StatusOK, startRes.Code)
	}
	if !strings.Contains(startRes.Body.String(), "setup_in_progress") {
		t.Fatalf("expected setup_in_progress response, got %q", startRes.Body.String())
	}

	completeReq := httptest.NewRequest(http.MethodPost, "/api/v1/setup/complete", nil)
	completeRes := httptest.NewRecorder()
	instance.NewMux().ServeHTTP(completeRes, completeReq)
	if completeRes.Code != http.StatusOK {
		t.Fatalf("expected setup complete status %d, got %d", http.StatusOK, completeRes.Code)
	}

	statusReq := httptest.NewRequest(http.MethodGet, "/api/v1/setup/status", nil)
	statusRes := httptest.NewRecorder()
	instance.NewMux().ServeHTTP(statusRes, statusReq)
	if !strings.Contains(statusRes.Body.String(), "setup_complete") {
		t.Fatalf("expected persisted setup_complete state, got %q", statusRes.Body.String())
	}
}
