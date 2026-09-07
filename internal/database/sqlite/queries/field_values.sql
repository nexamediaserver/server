-- name: UpsertFieldValue :exec
INSERT INTO field_values (
    object_id, field_id, position,
    text_value, integer_value, real_value, boolean_value, date_value, datetime_value, json_value
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
ON CONFLICT(object_id, field_id) DO UPDATE SET
    position = excluded.position,
    text_value = excluded.text_value,
    integer_value = excluded.integer_value,
    real_value = excluded.real_value,
    boolean_value = excluded.boolean_value,
    date_value = excluded.date_value,
    datetime_value = excluded.datetime_value,
    json_value = excluded.json_value;

-- name: GetFieldValuesForItem :many
SELECT id, object_id, field_id, position,
       text_value, integer_value, real_value, boolean_value, date_value, datetime_value, json_value
FROM field_values
WHERE object_id = ?
ORDER BY position, field_id;
