-- name: CreateFieldDefinition :exec
INSERT INTO field_definitions (
    id, library_id, namespace, key, display_name, field_type,
    required, position, configuration_json, validation_json, created_at, updated_at
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);

-- name: ListFieldDefinitionsForLibrary :many
SELECT id, library_id, namespace, key, display_name, field_type,
       required, position, configuration_json, validation_json, created_at, updated_at
FROM field_definitions
WHERE library_id = ?
ORDER BY position, id
LIMIT ?;
