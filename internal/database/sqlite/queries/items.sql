-- name: CreateItem :exec
INSERT INTO items (id, library_id, status, revision, created_at, updated_at)
VALUES (?, ?, ?, ?, ?, ?);

-- name: GetItem :one
SELECT id, library_id, status, revision, created_at, updated_at
FROM items
WHERE id = ?;

-- name: ListItemsForLibraryFirstPage :many
SELECT id, library_id, status, revision, created_at, updated_at
FROM items
WHERE library_id = ?
ORDER BY created_at, id
LIMIT ?;

-- name: ListItemsForLibraryAfterCursor :many
SELECT id, library_id, status, revision, created_at, updated_at
FROM items
WHERE library_id = ?
  AND (created_at > ? OR (created_at = ? AND id > ?))
ORDER BY created_at, id
LIMIT ?;
