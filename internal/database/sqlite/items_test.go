package sqlite

import (
	"context"
	"testing"
)

func TestDomainStoreCRUDAndPagination(t *testing.T) {
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

	library, err := store.GetLibrary(ctx, libraryID)
	if err != nil {
		t.Fatalf("GetLibrary returned error: %v", err)
	}
	if library.Slug != "movies" || library.Name != "Movies" {
		t.Fatalf("unexpected library: %+v", library)
	}

	libraries, err := store.ListLibraries(ctx, 10, 0)
	if err != nil {
		t.Fatalf("ListLibraries returned error: %v", err)
	}
	if len(libraries) != 1 || libraries[0].ID != libraryID {
		t.Fatalf("unexpected libraries: %+v", libraries)
	}

	titleFieldID, err := store.CreateFieldDefinition(ctx, FieldDefinition{
		LibraryID:   libraryID,
		Namespace:   "core",
		Key:         "title",
		DisplayName: "Title",
		FieldType:   FieldTypeText,
		Position:    0,
	})
	if err != nil {
		t.Fatalf("CreateFieldDefinition (title) returned error: %v", err)
	}

	yearFieldID, err := store.CreateFieldDefinition(ctx, FieldDefinition{
		LibraryID:   libraryID,
		Namespace:   "core",
		Key:         "year",
		DisplayName: "Year",
		FieldType:   FieldTypeInteger,
		Position:    1,
	})
	if err != nil {
		t.Fatalf("CreateFieldDefinition (year) returned error: %v", err)
	}

	fieldDefs, err := store.ListFieldDefinitionsForLibrary(ctx, libraryID, 10)
	if err != nil {
		t.Fatalf("ListFieldDefinitionsForLibrary returned error: %v", err)
	}
	if len(fieldDefs) != 2 {
		t.Fatalf("expected 2 field definitions, got %d", len(fieldDefs))
	}

	const itemCount = 5
	itemIDs := make([]string, 0, itemCount)
	for i := 0; i < itemCount; i++ {
		itemID, err := store.CreateItem(ctx, libraryID, "")
		if err != nil {
			t.Fatalf("CreateItem returned error: %v", err)
		}
		itemIDs = append(itemIDs, itemID)
	}

	item, err := store.GetItem(ctx, itemIDs[0])
	if err != nil {
		t.Fatalf("GetItem returned error: %v", err)
	}
	if item.LibraryID != libraryID || item.Revision != 1 || item.Status != "active" {
		t.Fatalf("unexpected item: %+v", item)
	}

	title := "The Matrix"
	year := int64(1999)
	if err := store.UpsertFieldValue(ctx, FieldValue{
		ObjectID: itemIDs[0],
		FieldID:  titleFieldID,
		Type:     FieldTypeText,
		Text:     &title,
	}); err != nil {
		t.Fatalf("UpsertFieldValue (text) returned error: %v", err)
	}
	if err := store.UpsertFieldValue(ctx, FieldValue{
		ObjectID: itemIDs[0],
		FieldID:  yearFieldID,
		Type:     FieldTypeInteger,
		Integer:  &year,
	}); err != nil {
		t.Fatalf("UpsertFieldValue (integer) returned error: %v", err)
	}

	values, err := store.GetFieldValuesForItem(ctx, itemIDs[0])
	if err != nil {
		t.Fatalf("GetFieldValuesForItem returned error: %v", err)
	}
	if len(values) != 2 {
		t.Fatalf("expected 2 field values, got %d", len(values))
	}
	for _, v := range values {
		switch v.FieldID {
		case titleFieldID:
			if v.Type != FieldTypeText || v.Text == nil || *v.Text != title {
				t.Fatalf("unexpected text value: %+v", v)
			}
			if v.Integer != nil || v.Boolean != nil {
				t.Fatalf("expected only text_value populated, got: %+v", v)
			}
		case yearFieldID:
			if v.Type != FieldTypeInteger || v.Integer == nil || *v.Integer != year {
				t.Fatalf("unexpected integer value: %+v", v)
			}
			if v.Text != nil || v.Boolean != nil {
				t.Fatalf("expected only integer_value populated, got: %+v", v)
			}
		default:
			t.Fatalf("unexpected field id: %s", v.FieldID)
		}
	}

	// re-upsert the same field to confirm update-in-place semantics.
	updatedTitle := "The Matrix Reloaded"
	if err := store.UpsertFieldValue(ctx, FieldValue{
		ObjectID: itemIDs[0],
		FieldID:  titleFieldID,
		Type:     FieldTypeText,
		Text:     &updatedTitle,
	}); err != nil {
		t.Fatalf("UpsertFieldValue (update) returned error: %v", err)
	}
	values, err = store.GetFieldValuesForItem(ctx, itemIDs[0])
	if err != nil {
		t.Fatalf("GetFieldValuesForItem returned error: %v", err)
	}
	if len(values) != 2 {
		t.Fatalf("expected 2 field values after update, got %d", len(values))
	}

	// keyset pagination across more than one page.
	seen := map[string]bool{}
	var cursor *ItemCursor
	pageSize := 2
	pages := 0
	for {
		page, next, err := store.ListItemsForLibrary(ctx, libraryID, pageSize, cursor)
		if err != nil {
			t.Fatalf("ListItemsForLibrary returned error: %v", err)
		}
		if len(page) == 0 {
			t.Fatalf("expected non-empty page %d", pages)
		}
		if len(page) > pageSize {
			t.Fatalf("page %d exceeded page size: got %d", pages, len(page))
		}
		for _, it := range page {
			if seen[it.ID] {
				t.Fatalf("item %s seen more than once across pages", it.ID)
			}
			seen[it.ID] = true
		}
		pages++
		if next == nil {
			break
		}
		cursor = next
		if pages > itemCount {
			t.Fatalf("pagination did not terminate")
		}
	}
	if len(seen) != itemCount {
		t.Fatalf("expected to see %d items across pages, got %d", itemCount, len(seen))
	}
	if pages < 2 {
		t.Fatalf("expected pagination across more than one page, got %d pages", pages)
	}
}
