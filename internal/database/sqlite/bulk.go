package sqlite

import (
	"context"
	"fmt"
)

// fieldValueBulkChunkSize is the number of field_values rows committed per
// transaction by BulkUpsertFieldValues. 500 keeps each transaction well
// short of the point where a single write would hold SQLite's write lock
// (and block every other reader/writer behind the write queue) for long
// enough to matter, while still amortizing per-transaction overhead across
// many rows instead of committing one row at a time.
const fieldValueBulkChunkSize = 500

// BulkUpsertFieldValues upserts many field values in bounded-size chunks,
// each committed as its own short transaction on the write queue, rather
// than one row per transaction (too many commits/fsyncs) or one transaction
// for the whole batch (a single long-held write lock that starves every
// other reader and writer for the duration of the import). This follows the
// "commit in bounded chunks rather than hold long transactions" guidance in
// docs/05-persistence-and-query.md.
func (s *DomainStore) BulkUpsertFieldValues(ctx context.Context, values []FieldValue) error {
	for start := 0; start < len(values); start += fieldValueBulkChunkSize {
		end := start + fieldValueBulkChunkSize
		if end > len(values) {
			end = len(values)
		}
		chunk := values[start:end]

		err := s.writes.submit(ctx, func(ctx context.Context) error {
			return s.upsertFieldValueChunk(ctx, chunk)
		})
		if err != nil {
			return fmt.Errorf("bulk upsert field values [%d:%d) of %d: %w", start, end, len(values), err)
		}
	}
	return nil
}

// upsertFieldValueChunk upserts one chunk inside a single transaction. It
// must only be called on the write queue's worker goroutine: it is not
// itself concurrency-safe against other writers.
func (s *DomainStore) upsertFieldValueChunk(ctx context.Context, chunk []FieldValue) error {
	tx, err := s.sqlDB.BeginTx(ctx, nil)
	if err != nil {
		return fmt.Errorf("begin transaction: %w", err)
	}
	defer func() { _ = tx.Rollback() }()

	q := s.queries.WithTx(tx)
	for _, v := range chunk {
		params, err := fieldValueParams(v)
		if err != nil {
			return err
		}
		if err := q.UpsertFieldValue(ctx, params); err != nil {
			return fmt.Errorf("upsert field value for item %q field %q: %w", v.ObjectID, v.FieldID, err)
		}
	}

	if err := tx.Commit(); err != nil {
		return fmt.Errorf("commit transaction: %w", err)
	}
	return nil
}
