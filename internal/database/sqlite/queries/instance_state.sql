-- name: GetInstanceState :one
SELECT setup_state FROM instance_state WHERE id = 1;

-- name: UpsertInstanceState :exec
INSERT INTO instance_state (id, setup_state) VALUES (1, ?)
ON CONFLICT(id) DO UPDATE SET setup_state = excluded.setup_state, updated_at = CURRENT_TIMESTAMP;
