import { useState } from "react";
import { getActorName, setActorName } from "../api/actor";

export function ActorNameInput() {
  const [name, setName] = useState(getActorName());

  return (
    <label className="flex items-center gap-2 text-sm text-slate-300">
      Your name
      <input
        className="w-36 rounded border border-slate-600 bg-slate-800 px-2 py-1 text-slate-100 focus:border-sky-500 focus:outline-none"
        value={name}
        placeholder="e.g. Himanshu"
        onChange={(e) => {
          setName(e.target.value);
          setActorName(e.target.value);
        }}
      />
    </label>
  );
}
