const STORAGE_KEY = "mealpenalty.actorName";
const DEFAULT_ACTOR_NAME = "Guest";

export function getActorName(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) || DEFAULT_ACTOR_NAME;
  } catch {
    return DEFAULT_ACTOR_NAME;
  }
}

export function setActorName(name: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, name);
  } catch {
    // localStorage unavailable (private mode, etc.) - actor name just won't persist across reloads.
  }
}
