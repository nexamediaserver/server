package sqlite

//go:generate go tool sqlc generate -f ../../../sqlc.yaml

import (
	"context"
	"database/sql"
	"embed"
	"fmt"
	"path/filepath"
	"time"

	"github.com/golang-migrate/migrate/v4"
	migratesqlite "github.com/golang-migrate/migrate/v4/database/sqlite"
	"github.com/golang-migrate/migrate/v4/source/iofs"
	_ "modernc.org/sqlite"

	"nexa/internal/bootstrap"
	"nexa/internal/database/sqlite/sqlcgen"
)

const driverName = "sqlite"

//go:embed migrations/*.sql
var migrationFiles embed.FS

type Database struct {
	*sql.DB
	writes *writeQueue
}

func Open(dataDir string) (*Database, error) {
	path := filepath.Join(dataDir, "nexa.db")

	// journal_mode, synchronous, foreign_keys and busy_timeout are per-
	// connection settings in SQLite: journal_mode is persisted in the
	// database file once set (so it survives across connections), but
	// synchronous, foreign_keys and busy_timeout are not. A single
	// db.Exec("PRAGMA ...") after sql.Open only configures whichever one
	// connection happens to serve that call; every other connection the
	// pool later opens (e.g. for a concurrent reader) would silently run
	// with foreign_keys off and busy_timeout 0, causing immediate
	// SQLITE_BUSY errors instead of the intended retry-with-timeout
	// behavior. Passing these as DSN query parameters makes the driver
	// apply them on every new connection it opens, not just the first.
	dsn := fmt.Sprintf("%s?_journal_mode=WAL&_synchronous=NORMAL&_foreign_keys=on&_busy_timeout=5000", path)
	db, err := sql.Open(driverName, dsn)
	if err != nil {
		return nil, fmt.Errorf("open sqlite database: %w", err)
	}

	// SQLite (even in WAL mode) allows many concurrent readers but
	// serializes writers at the file level, so an unbounded pool (the
	// database/sql default) just lets more goroutines pile up blocked
	// inside SQLite's busy-timeout retry loop, each holding a pooled
	// connection idle while it waits. Bounding the pool keeps queuing in
	// Go instead of in SQLite retries. 8 open connections is enough for
	// several read requests to run in parallel under WAL without
	// materially increasing memory use (each connection keeps its own
	// page cache), while writes are additionally serialized in-process
	// by writeQueue (see writequeue.go), so this limit mostly governs
	// read concurrency, not write throughput. MaxIdleConns matches
	// MaxOpenConns so the pool does not churn connections open/closed
	// under bursty load; ConnMaxIdleTime reclaims them after a period of
	// inactivity so an idle server does not hold the maximum forever.
	db.SetMaxOpenConns(8)
	db.SetMaxIdleConns(8)
	db.SetConnMaxIdleTime(5 * time.Minute)

	if err := db.Ping(); err != nil {
		_ = db.Close()
		return nil, fmt.Errorf("ping sqlite database: %w", err)
	}

	return &Database{DB: db, writes: newWriteQueue()}, nil
}

// Close stops the write queue's worker goroutine and closes the underlying
// database connections. It shadows the promoted *sql.DB.Close so both are
// released together.
func (db *Database) Close() error {
	db.writes.close()
	return db.DB.Close()
}

func (db *Database) Migrate(ctx context.Context) error {
	_ = ctx

	source, err := iofs.New(migrationFiles, "migrations")
	if err != nil {
		return fmt.Errorf("load migrations: %w", err)
	}

	driver, err := migratesqlite.WithInstance(db.DB, &migratesqlite.Config{})
	if err != nil {
		return fmt.Errorf("create migration driver: %w", err)
	}

	migrator, err := migrate.NewWithInstance("iofs", source, "sqlite", driver)
	if err != nil {
		return fmt.Errorf("create migrator: %w", err)
	}

	if err := migrator.Up(); err != nil && err != migrate.ErrNoChange {
		return fmt.Errorf("apply migrations: %w", err)
	}

	return nil
}

type StateStore struct {
	queries *sqlcgen.Queries
}

func NewStateStore(db *Database) *StateStore {
	return &StateStore{queries: sqlcgen.New(db.DB)}
}

func (s *StateStore) Read() (bootstrap.State, error) {
	value, err := s.queries.GetInstanceState(context.Background())
	if err != nil {
		if err == sql.ErrNoRows {
			return bootstrap.StateUninitialized, nil
		}
		return "", fmt.Errorf("read setup state: %w", err)
	}
	return bootstrap.State(value), nil
}

func (s *StateStore) Write(state bootstrap.State) error {
	if err := s.queries.UpsertInstanceState(context.Background(), string(state)); err != nil {
		return fmt.Errorf("write setup state: %w", err)
	}
	return nil
}
