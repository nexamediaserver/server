package sqlite

import (
	"context"
	"database/sql"
	"fmt"

	"nexa/internal/database/sqlite/sqlcgen"
)

// FieldValue is a single typed value of one field on one item. Exactly one
// of the typed pointer fields is populated, matching Type; the others are
// nil. This mirrors the single field_values table with nullable typed
// columns described in docs/05-persistence-and-query.md.
type FieldValue struct {
	ObjectID string
	FieldID  string
	Position int
	Type     FieldType

	Text     *string
	Integer  *int64
	Real     *float64
	Boolean  *bool
	Date     *string
	DateTime *string
	JSON     *string
}

func fieldValueFromRow(row sqlcgen.FieldValue) FieldValue {
	fv := FieldValue{
		ObjectID: row.ObjectID,
		FieldID:  row.FieldID,
		Position: int(row.Position),
	}
	switch {
	case row.TextValue.Valid:
		fv.Type = FieldTypeText
		fv.Text = &row.TextValue.String
	case row.IntegerValue.Valid:
		fv.Type = FieldTypeInteger
		fv.Integer = &row.IntegerValue.Int64
	case row.RealValue.Valid:
		fv.Type = FieldTypeReal
		fv.Real = &row.RealValue.Float64
	case row.BooleanValue.Valid:
		fv.Type = FieldTypeBoolean
		b := row.BooleanValue.Int64 != 0
		fv.Boolean = &b
	case row.DateValue.Valid:
		fv.Type = FieldTypeDate
		fv.Date = &row.DateValue.String
	case row.DatetimeValue.Valid:
		fv.Type = FieldTypeDateTime
		fv.DateTime = &row.DatetimeValue.String
	case row.JsonValue.Valid:
		fv.Type = FieldTypeJSON
		fv.JSON = &row.JsonValue.String
	}
	return fv
}

// UpsertFieldValue inserts or replaces the value of one field on one item.
// Only the SQL column matching v.Type is populated; all other typed columns
// are stored as NULL. The caller must set the pointer field corresponding to
// v.Type. The write runs on the database's write queue so concurrent callers
// serialize safely instead of contending for SQLite's write lock.
func (s *DomainStore) UpsertFieldValue(ctx context.Context, v FieldValue) error {
	params, err := fieldValueParams(v)
	if err != nil {
		return err
	}
	err = s.writes.submit(ctx, func(ctx context.Context) error {
		return s.queries.UpsertFieldValue(ctx, params)
	})
	if err != nil {
		return fmt.Errorf("upsert field value for item %q field %q: %w", v.ObjectID, v.FieldID, err)
	}
	return nil
}

// fieldValueParams converts a FieldValue into sqlc upsert params, validating
// that the pointer field matching v.Type is populated. It is shared by
// UpsertFieldValue and the bulk chunk helper in bulk.go so both apply
// identical validation.
func fieldValueParams(v FieldValue) (sqlcgen.UpsertFieldValueParams, error) {
	params := sqlcgen.UpsertFieldValueParams{
		ObjectID: v.ObjectID,
		FieldID:  v.FieldID,
		Position: int64(v.Position),
	}

	switch v.Type {
	case FieldTypeText:
		if v.Text == nil {
			return params, fmt.Errorf("upsert field value: type %q requires Text", v.Type)
		}
		params.TextValue = sql.NullString{String: *v.Text, Valid: true}
	case FieldTypeInteger:
		if v.Integer == nil {
			return params, fmt.Errorf("upsert field value: type %q requires Integer", v.Type)
		}
		params.IntegerValue = sql.NullInt64{Int64: *v.Integer, Valid: true}
	case FieldTypeReal:
		if v.Real == nil {
			return params, fmt.Errorf("upsert field value: type %q requires Real", v.Type)
		}
		params.RealValue = sql.NullFloat64{Float64: *v.Real, Valid: true}
	case FieldTypeBoolean:
		if v.Boolean == nil {
			return params, fmt.Errorf("upsert field value: type %q requires Boolean", v.Type)
		}
		b := int64(0)
		if *v.Boolean {
			b = 1
		}
		params.BooleanValue = sql.NullInt64{Int64: b, Valid: true}
	case FieldTypeDate:
		if v.Date == nil {
			return params, fmt.Errorf("upsert field value: type %q requires Date", v.Type)
		}
		params.DateValue = sql.NullString{String: *v.Date, Valid: true}
	case FieldTypeDateTime:
		if v.DateTime == nil {
			return params, fmt.Errorf("upsert field value: type %q requires DateTime", v.Type)
		}
		params.DatetimeValue = sql.NullString{String: *v.DateTime, Valid: true}
	case FieldTypeJSON:
		if v.JSON == nil {
			return params, fmt.Errorf("upsert field value: type %q requires JSON", v.Type)
		}
		params.JsonValue = sql.NullString{String: *v.JSON, Valid: true}
	default:
		return params, fmt.Errorf("upsert field value: unknown field type %q", v.Type)
	}

	return params, nil
}

// GetFieldValuesForItem returns all field values for a single item. This is
// bounded by the number of field definitions on the item's library, not by
// the size of the field_values table, so it does not require pagination.
func (s *DomainStore) GetFieldValuesForItem(ctx context.Context, itemID string) ([]FieldValue, error) {
	rows, err := s.queries.GetFieldValuesForItem(ctx, itemID)
	if err != nil {
		return nil, fmt.Errorf("get field values for item %q: %w", itemID, err)
	}
	values := make([]FieldValue, 0, len(rows))
	for _, row := range rows {
		values = append(values, fieldValueFromRow(row))
	}
	return values, nil
}
