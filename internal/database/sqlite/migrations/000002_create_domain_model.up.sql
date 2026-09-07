CREATE TABLE IF NOT EXISTS libraries (
    id TEXT PRIMARY KEY,
    slug TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    content_type TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS field_definitions (
    id TEXT PRIMARY KEY,
    library_id TEXT NOT NULL REFERENCES libraries(id) ON DELETE CASCADE,
    namespace TEXT NOT NULL,
    key TEXT NOT NULL,
    display_name TEXT NOT NULL,
    field_type TEXT NOT NULL,
    required INTEGER NOT NULL DEFAULT 0,
    position INTEGER NOT NULL DEFAULT 0,
    configuration_json TEXT,
    validation_json TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    UNIQUE (library_id, namespace, key)
);

CREATE TABLE IF NOT EXISTS items (
    id TEXT PRIMARY KEY,
    library_id TEXT NOT NULL REFERENCES libraries(id) ON DELETE CASCADE,
    status TEXT NOT NULL DEFAULT 'active',
    revision INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

-- Supports keyset pagination scoped to a library: WHERE library_id = ? AND
-- (created_at, id) > (?, ?) ORDER BY created_at, id LIMIT ?. created_at
-- orders items chronologically for callers; id breaks ties between items
-- created in the same instant so the cursor is always strictly increasing.
CREATE INDEX IF NOT EXISTS idx_items_library_created_id ON items(library_id, created_at, id);

CREATE TABLE IF NOT EXISTS field_values (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    object_id TEXT NOT NULL REFERENCES items(id) ON DELETE CASCADE,
    field_id TEXT NOT NULL REFERENCES field_definitions(id) ON DELETE CASCADE,
    position INTEGER NOT NULL DEFAULT 0,
    text_value TEXT,
    integer_value INTEGER,
    real_value REAL,
    boolean_value INTEGER,
    date_value TEXT,
    datetime_value TEXT,
    json_value TEXT,
    -- One value row per field per item; also serves as the lookup index for
    -- "all values of one item" (object_id) and "one field of one item"
    -- (object_id, field_id) access patterns, so no extra object_id index is needed.
    UNIQUE (object_id, field_id)
);

-- One index per scalar column that the future query engine filters/sorts on,
-- as recommended by docs/05-persistence-and-query.md. json_value is an
-- unstructured escape hatch and is intentionally not indexed for filtering.
CREATE INDEX IF NOT EXISTS idx_field_values_field_text ON field_values(field_id, text_value);
CREATE INDEX IF NOT EXISTS idx_field_values_field_integer ON field_values(field_id, integer_value);
CREATE INDEX IF NOT EXISTS idx_field_values_field_real ON field_values(field_id, real_value);
CREATE INDEX IF NOT EXISTS idx_field_values_field_boolean ON field_values(field_id, boolean_value);
CREATE INDEX IF NOT EXISTS idx_field_values_field_date ON field_values(field_id, date_value);
CREATE INDEX IF NOT EXISTS idx_field_values_field_datetime ON field_values(field_id, datetime_value);
