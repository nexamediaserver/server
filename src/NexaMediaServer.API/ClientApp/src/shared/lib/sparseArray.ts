/**
 * Type guard to check if an item is an unloaded placeholder (null).
 * When Apollo merges offset collections, it creates sparse arrays where
 * unloaded positions are filled with null. The read function preserves
 * these nulls so they can be detected by this guard.
 */
export const isUnloadedItem = (item: unknown): item is null => item === null
