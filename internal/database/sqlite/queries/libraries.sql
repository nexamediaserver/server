-- name: CreateLibrary :exec
INSERT INTO libraries (id, slug, name, content_type, created_at, updated_at)
VALUES (?, ?, ?, ?, ?, ?);

-- name: GetLibrary :one
SELECT id, slug, name, content_type, created_at, updated_at
FROM libraries
WHERE id = ?;

-- name: ListLibraries :many
SELECT id, slug, name, content_type, created_at, updated_at
FROM libraries
ORDER BY name, id
LIMIT ? OFFSET ?;
