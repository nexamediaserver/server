package sqlite

import (
	"database/sql"
	"time"

	"github.com/google/uuid"

	"nexa/internal/database/sqlite/sqlcgen"
)

// DomainStore is a hand-written repository over the minimal domain model
// (libraries, field definitions, items, field values). It mirrors StateStore:
// a thin wrapper around sqlc-generated queries that exposes domain-shaped
// Go types instead of sqlcgen rows to callers.
//
// Mutating methods run on writes, the database's single-writer queue, so
// concurrent callers cannot thrash against each other with SQLITE_BUSY (see
// writequeue.go). sqlDB is kept alongside queries so bulk helpers can open
// their own bounded-chunk transactions (see bulk.go).
type DomainStore struct {
	queries *sqlcgen.Queries
	sqlDB   *sql.DB
	writes  *writeQueue
}

func NewDomainStore(db *Database) *DomainStore {
	return &DomainStore{queries: sqlcgen.New(db.DB), sqlDB: db.DB, writes: db.writes}
}

// newID returns a new opaque, immutable identifier for a domain entity.
func newID() string {
	return uuid.NewString()
}

// timestampLayout is a fixed-width RFC3339 variant (always 9 fractional
// digits, always UTC). Timestamps are stored as TEXT and are compared both
// for equality and ordering (e.g. keyset pagination), so the layout must be
// lexically sortable in the same order as chronological order. Using a
// variable-width layout such as time.RFC3339Nano would break this: it trims
// trailing zero fractional digits, so "...01Z" (no fraction) can sort before
// "...01.5Z" (with fraction) even though it is chronologically later.
const timestampLayout = "2006-01-02T15:04:05.000000000Z"

func nowTimestamp() string {
	return time.Now().UTC().Format(timestampLayout)
}

// boundLimit clamps a caller-supplied page size to (0, max], substituting a
// default when the caller does not specify one. This keeps every listing
// query bounded regardless of what a caller passes in, per the requirement
// that the server must never run unbounded scans against large libraries.
func boundLimit(limit, def, max int) int64 {
	switch {
	case limit <= 0:
		return int64(def)
	case limit > max:
		return int64(max)
	default:
		return int64(limit)
	}
}
