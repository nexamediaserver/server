package sqlite

import (
	"context"
	"database/sql"
	"fmt"

	"nexa/internal/database/sqlite/sqlcgen"
)

const (
	defaultFieldDefinitionPageSize = 200
	maxFieldDefinitionPageSize     = 1000
)

// FieldType identifies which scalar column of field_values a field's values
// are stored in.
type FieldType string

const (
	FieldTypeText     FieldType = "text"
	FieldTypeInteger  FieldType = "integer"
	FieldTypeReal     FieldType = "real"
	FieldTypeBoolean  FieldType = "boolean"
	FieldTypeDate     FieldType = "date"
	FieldTypeDateTime FieldType = "datetime"
	FieldTypeJSON     FieldType = "json"
)

// FieldDefinition describes a runtime-defined field on items belonging to a
// library.
type FieldDefinition struct {
	ID                string
	LibraryID         string
	Namespace         string
	Key               string
	DisplayName       string
	FieldType         FieldType
	Required          bool
	Position          int
	ConfigurationJSON string
	ValidationJSON    string
	CreatedAt         string
	UpdatedAt         string
}

func fieldDefinitionFromRow(row sqlcgen.FieldDefinition) FieldDefinition {
	return FieldDefinition{
		ID:                row.ID,
		LibraryID:         row.LibraryID,
		Namespace:         row.Namespace,
		Key:               row.Key,
		DisplayName:       row.DisplayName,
		FieldType:         FieldType(row.FieldType),
		Required:          row.Required != 0,
		Position:          int(row.Position),
		ConfigurationJSON: row.ConfigurationJson.String,
		ValidationJSON:    row.ValidationJson.String,
		CreatedAt:         row.CreatedAt,
		UpdatedAt:         row.UpdatedAt,
	}
}

// CreateFieldDefinition creates a new field definition scoped to a library
// and returns its generated ID. (library_id, namespace, key) is enforced
// unique by the schema so custom fields cannot collide. The insert runs on
// the database's write queue so concurrent callers serialize safely instead
// of contending for SQLite's write lock.
func (s *DomainStore) CreateFieldDefinition(ctx context.Context, def FieldDefinition) (string, error) {
	id := newID()
	now := nowTimestamp()
	required := int64(0)
	if def.Required {
		required = 1
	}
	err := s.writes.submit(ctx, func(ctx context.Context) error {
		return s.queries.CreateFieldDefinition(ctx, sqlcgen.CreateFieldDefinitionParams{
			ID:                id,
			LibraryID:         def.LibraryID,
			Namespace:         def.Namespace,
			Key:               def.Key,
			DisplayName:       def.DisplayName,
			FieldType:         string(def.FieldType),
			Required:          required,
			Position:          int64(def.Position),
			ConfigurationJson: nullString(def.ConfigurationJSON),
			ValidationJson:    nullString(def.ValidationJSON),
			CreatedAt:         now,
			UpdatedAt:         now,
		})
	})
	if err != nil {
		return "", fmt.Errorf("create field definition %s/%s: %w", def.Namespace, def.Key, err)
	}
	return id, nil
}

// ListFieldDefinitionsForLibrary returns field definitions for a library
// ordered by display position. A field definition list is bounded by
// configuration, not data volume, so a single bounded query is sufficient
// without cursor pagination.
func (s *DomainStore) ListFieldDefinitionsForLibrary(ctx context.Context, libraryID string, limit int) ([]FieldDefinition, error) {
	rows, err := s.queries.ListFieldDefinitionsForLibrary(ctx, sqlcgen.ListFieldDefinitionsForLibraryParams{
		LibraryID: libraryID,
		Limit:     boundLimit(limit, defaultFieldDefinitionPageSize, maxFieldDefinitionPageSize),
	})
	if err != nil {
		return nil, fmt.Errorf("list field definitions for library %q: %w", libraryID, err)
	}
	defs := make([]FieldDefinition, 0, len(rows))
	for _, row := range rows {
		defs = append(defs, fieldDefinitionFromRow(row))
	}
	return defs, nil
}

func nullString(v string) sql.NullString {
	return sql.NullString{String: v, Valid: v != ""}
}
