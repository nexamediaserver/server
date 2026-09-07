package main

import (
	"log"
	"net/http"
	"os"
	"time"

	"nexa/internal/app"
	"nexa/internal/bootstrap"
)

func main() {
	dataDir := bootstrap.DefaultDataDir()
	instance, err := app.NewApp(dataDir)
	if err != nil {
		log.Fatalf("initializing app: %v", err)
	}

	addr := getAddr()
	mux := instance.NewMux()

	srv := &http.Server{
		Addr:              addr,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       10 * time.Second,
		WriteTimeout:      10 * time.Second,
		IdleTimeout:       15 * time.Second,
	}

	log.Printf("nexa server listening on %s (data dir: %s)", addr, dataDir)
	if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		log.Fatalf("server failed: %v", err)
	}
}

func getAddr() string {
	if addr := os.Getenv("NEXA_ADDR"); addr != "" {
		return addr
	}
	return ":8321"
}
