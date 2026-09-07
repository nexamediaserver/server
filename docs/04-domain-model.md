# Domain Model

## Overview

The Nexa domain model is intentionally generic. It must support media/content types not known when core is compiled.

The key concepts are:

- Library/Hub
- Item/Object
- Field Definition and Field Value
- Agent and Agent Kind
- Credit and Role
- Relationships
- Media Asset
- View
- User

## Libraries / Hubs

A library is a configured content domain.

Typical examples:

- Movies
- Television
- Music
- Games
- Books
- Comics

These examples are provided by extensions, not hardcoded core enums.

A library contains configuration such as:

```text
id
slug
name
icon
content type/template
field definitions
views
permissions
metadata configuration
storage/source configuration
extension-specific settings
```

Creating or modifying a library must not require a database schema migration.

## Items / Objects

An Item is a record belonging to a library/hub.

Core properties should remain minimal:

```text
id
library_id
created_at
updated_at
revision
lifecycle/status metadata
```

Domain-specific values come from fields and relationships.

The architecture may later generalize Item into a broader Object abstraction. Until that decision is finalized, public contracts should avoid assuming that every field-bearing entity is necessarily an Item.

## Fields

A Field Definition describes a runtime-defined field.

Minimum conceptual properties:

```text
id
namespace/key
display name
field type
target object class
required
configuration
validation
indexing/query hints
position/display metadata
```

Field values always reference immutable field IDs, not labels.

Field types are registered capabilities. Core provides basic scalar types; extensions may add domain-specific field types.

Core field types are expected to include:

- short text;
- long text;
- integer;
- decimal;
- boolean;
- date;
- datetime;
- URL;
- email;
- enum;
- multi-enum;
- relation;
- file/media asset;
- image/media asset;
- user reference.

## Agents

The core abstraction is `Agent`, not `Person`.

An Agent represents an entity that can participate in credits or relationships:

- individual person;
- group of persons;
- company;
- organization;
- studio;
- record label;
- publisher;
- band;
- orchestra;
- team;
- collective;
- extension-defined entity kind.

The UI may use friendlier labels such as "People & Organizations".

### Agent identity

Conceptual core properties:

```text
id
kind_id
primary/display name
created_at
updated_at
revision
```

Agents should use the same custom-field machinery as content records wherever practical.

### Agent kinds

Agent kinds are runtime-extensible and namespaced.

Examples:

```text
core.person
core.group
core.organization

music.band
music.orchestra
music.record_label

games.developer
games.publisher

movies.production_company
books.publisher
```

An agent kind may expose broader capabilities/semantic parents such as `organization-like` without requiring language-level inheritance.

Do not force a universal taxonomy that makes a record label and game publisher artificially identical. Generic behavior comes from capabilities; domain meaning remains namespaced.

### Names and aliases

Agents need multiple names.

Conceptual model:

```text
agent_names
-----------
agent_id
name
name_type
language
script
is_primary
valid_from?
valid_to?
```

This supports:

- aliases;
- stage names;
- pseudonyms;
- historical names;
- translated/transliterated names;
- localized display selection.

## Credits

A role is not a property of an Agent. It is the meaning of a relationship between an Item/Object and an Agent.

Conceptual credit:

```text
id
subject_object_id
agent_id
role_id
credited_as
position
metadata
```

Roles are namespaced and extension-defined.

Examples:

```text
movies.director
movies.actor
music.artist
music.composer
music.label
games.developer
games.publisher
books.author
books.translator
```

`credited_as` preserves the exact name used for a specific work.

## Agent-to-agent relationships

Groups, companies, and organizations require explicit relationships.

Examples:

- `member_of`;
- `subsidiary_of`;
- `imprint_of`;
- `successor_of`;
- extension-defined relation types.

Conceptual relationship:

```text
source_agent_id
target_agent_id
relation_type_id
valid_from
valid_to
metadata
```

Temporal validity is important for band membership and corporate history.

## Item/Object relationships

Content-to-content relationships must also be first class.

Examples:

- episode belongs to season;
- season belongs to series;
- sequel/prequel;
- track belongs to disc/album;
- adaptation of;
- alternate version;
- collection membership.

The exact physical storage may share infrastructure with generic relationships, but relation semantics remain typed and namespaced.

## Views

A View is a saved presentation/query configuration over a library or compatible object scope.

It may include:

```text
columns/fields
filter AST
sorting
grouping
pagination defaults
display type
display-specific configuration
```

Display types may include:

- table;
- cards;
- gallery;
- kanban;
- calendar;
- timeline;
- extension-defined views.

Views must not own duplicated canonical content data.

## Identity rules

All persistent entities use immutable stable IDs.

Renaming:

- a field;
- agent;
- library;
- role;
- relation type;
- view;
- extension-provided semantic resource

must not break references.

Use namespaced symbolic IDs for extension-defined registries and opaque immutable IDs for instances.
