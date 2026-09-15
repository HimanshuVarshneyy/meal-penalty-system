const STORAGE_KEY = "mealpenalty.actorName";

export function getActorName(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? "";
  } catch {
    return "";
  }
}

export function setActorName(name: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, name);
  } catch {
    // localStorage unavailable (private mode, etc.) - actor name just won't persist across reloads.
  }
}
