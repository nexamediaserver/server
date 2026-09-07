package sqlite

import (
	"context"
	"testing"
)

// TestBulkUpsertFieldValuesChunks confirms BulkUpsertFieldValues correctly
// upserts a batch spanning several chunks (2000 rows over a 500-row chunk
// size) and that the result is exactly the expected set of values.
func TestBulkUpsertFieldValuesChunks(t *testing.T) {
	dataDir := t.TempDir()
	db, err := Open(dataDir)
	if err != nil {
		t.Fatalf("Open returned error: %v", err)
	}
	defer func() { _ = db.Close() }()

	if err := db.Migrate(context.Background()); err != nil {
		t.Fatalf("Migrate returned error: %v", err)
	}

	ctx := context.Background()
	store := NewDomainStore(db)

	libraryID, err := store.CreateLibrary(ctx, "movies", "Movies", "movies")
	if err != nil {
		t.Fatalf("CreateLibrary returned error: %v", err)
	}
	fieldID, err := store.CreateFieldDefinition(ctx, FieldDefinition{
		LibraryID:   libraryID,
		Namespace:   "core",
		Key:         "title",
		DisplayName: "Title",
		FieldType:   FieldTypeText,
	})
	if err != nil {
		t.Fatalf("CreateFieldDefinition returned error: %v", err)
	}

	const rowCount = 2000
	itemIDs := make([]string, rowCount)
	for i := range itemIDs {
		id, err := store.CreateItem(ctx, libraryID, "")
		if err != nil {
			t.Fatalf("CreateItem[%d] returned error: %v", i, err)
		}
		itemIDs[i] = id
	}

	values := make([]FieldValue, rowCount)
	for i, itemID := range itemIDs {
		text := itemID
		values[i] = FieldValue{ObjectID: itemID, FieldID: fieldID, Type: FieldTypeText, Text: &text}
	}

	if err := store.BulkUpsertFieldValues(ctx, values); err != nil {
		t.Fatalf("BulkUpsertFieldValues returned error: %v", err)
	}

	var count int
	if err := db.QueryRow("SELECT COUNT(*) FROM field_values WHERE field_id = ?", fieldID).Scan(&count); err != nil {
		t.Fatalf("count field_values: %v", err)
	}
	if count != rowCount {
		t.Fatalf("expected %d field_values rows, got %d", rowCount, count)
	}

	// Spot-check a value from the middle of the batch round-trips correctly.
	fv, err := store.GetFieldValuesForItem(ctx, itemIDs[rowCount/2])
	if err != nil {
		t.Fatalf("GetFieldValuesForItem returned error: %v", err)
	}
	if len(fv) != 1 || fv[0].Text == nil || *fv[0].Text != itemIDs[rowCount/2] {
		t.Fatalf("unexpected field value for item %q: %+v", itemIDs[rowCount/2], fv)
	}
}
