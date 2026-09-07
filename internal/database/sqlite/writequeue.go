package sqlite

import "context"

// writeQueue serializes write operations against a single SQLite database
// file onto one logical writer goroutine. SQLite only ever allows one writer
// at a time; without coordination, concurrent callers each grab a pooled
// connection and race to begin a write, and the losers sit inside SQLite's
// own busy-timeout retry loop holding a connection idle until they either
// acquire the write lock or time out. Funneling writes through a single
// worker instead means at most one write is ever in flight against SQLite,
// other writers wait cheaply on a Go channel (no connection consumed, no
// SQLITE_BUSY retries), and reads keep using the normal pooled connections
// concurrently and are unaffected.
type writeQueue struct {
	jobs chan writeJob
}

type writeJob struct {
	ctx  context.Context
	fn   func(ctx context.Context) error
	done chan error
}

// newWriteQueue starts the single writer goroutine and returns a queue ready
// to accept work.
func newWriteQueue() *writeQueue {
	q := &writeQueue{jobs: make(chan writeJob, 64)}
	go q.run()
	return q
}

func (q *writeQueue) run() {
	for job := range q.jobs {
		job.done <- job.fn(job.ctx)
	}
}

// submit runs fn on the single writer goroutine and blocks until it
// completes or ctx is cancelled first. fn should perform exactly one bounded
// write (a single statement or a short transaction) per the "keep write
// transactions short" guidance in docs/05-persistence-and-query.md.
func (q *writeQueue) submit(ctx context.Context, fn func(ctx context.Context) error) error {
	job := writeJob{ctx: ctx, fn: fn, done: make(chan error, 1)}
	select {
	case q.jobs <- job:
	case <-ctx.Done():
		return ctx.Err()
	}
	select {
	case err := <-job.done:
		return err
	case <-ctx.Done():
		return ctx.Err()
	}
}

// close stops the writer goroutine. Callers must ensure no submit calls are
// in flight or start after close is called.
func (q *writeQueue) close() {
	close(q.jobs)
}
