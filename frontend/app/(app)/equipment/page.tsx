import { cookies } from "next/headers";

import { Button } from "@/components/ui/button";
import { Card, CardHeader } from "@/components/ui/card";
import { PageHeader } from "@/components/ui/page-header";
import { API_URL } from "@/lib/api";
import { getDictionary } from "@/lib/i18n";
import { allowExerciseAgain } from "./actions";
import { EquipmentForm, type Group, type Item } from "./equipment-form";
import { EQUIPMENT_GROUPS, groupOf } from "./equipment-groups";
import { Suggestions, type Gap, type Suggestion } from "./suggestions";

type Equipment = { items: string[]; vocabulary: string[] };
type SuggestionPayload = {
  suggestions: Suggestion[];
  catalogueGaps: Gap[];
  totalCatalogueGaps: number;
  /** Logged entries, not distinct movements -- the two answer different questions. */
  explainedExercises: number;
  unexplainedExercises: number;
};
type Preferences = { excluded: { exerciseId: string; title: string }[] };

async function read<T>(path: string): Promise<T | null> {
  const cookieStore = await cookies();
  const response = await fetch(`${API_URL}${path}`, {
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  return response.ok ? ((await response.json()) as T) : null;
}

export default async function EquipmentPage() {
  const [dict, equipment, preferences, suggested] = await Promise.all([
    getDictionary(),
    read<Equipment>("/training/equipment"),
    read<Preferences>("/training/preferences"),
    read<SuggestionPayload>("/training/equipment/suggestions"),
  ]);

  const strings = dict.equipment;
  const owned = new Set(equipment?.items ?? []);

  // The vocabulary travels with the answer, so this screen never hardcodes a list that would
  // drift from the API's enum. What it does own is the words.
  const items: Item[] = (equipment?.vocabulary ?? []).map((value) => ({
    value,
    label: strings.items[value as keyof typeof strings.items] ?? value,
    owned: owned.has(value),
    group: groupOf(value),
  }));

  // Empty sections are dropped rather than rendered blank: a heading with nothing under it reads
  // as something failing to load.
  const groups: Group[] = EQUIPMENT_GROUPS.map((key) => ({
    key,
    label: strings.groups[key],
    items: items.filter((item) => item.group === key),
  })).filter((group) => group.items.length > 0);

  return (
    <>
      <PageHeader title={strings.title} lead={strings.lead} />

      <div className="flex flex-col gap-8">
        <EquipmentForm
          groups={groups}
          strings={{
            itemsLabel: strings.itemsLabel,
            itemsHint: strings.itemsHint,
            groups: strings.groups,
            save: strings.save,
            saving: strings.saving,
            saved: strings.saved,
          }}
        />

        <Card>
          <CardHeader title={strings.excludedTitle} />

          {preferences?.excluded.length ? (
            <ul className="flex flex-col divide-y divide-line" data-testid="excluded-list">
              {preferences.excluded.map((exercise) => (
                <li
                  key={exercise.exerciseId}
                  className="flex items-center justify-between gap-4 py-2.5 first:pt-0 last:pb-0"
                >
                  <span className="text-sm text-ink">{exercise.title}</span>
                  <form action={allowExerciseAgain}>
                    <input type="hidden" name="exerciseId" value={exercise.exerciseId} />
                    <Button
                      type="submit"
                      variant="ghost"
                      size="sm"
                      data-testid={`allow-${exercise.exerciseId}`}
                    >
                      {strings.removeExclusion}
                    </Button>
                  </form>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-ink-muted" data-testid="excluded-empty">
              {strings.excludedEmpty}
            </p>
          )}
        </Card>

        {suggested && (
          <Suggestions
            suggestions={suggested.suggestions}
            gaps={suggested.catalogueGaps}
            strings={{
              title: dict.hevy.suggestionsTitle,
              lead: dict.hevy.suggestionsLead,
              accept: dict.hevy.accept,
              decline: dict.hevy.decline,
              empty: dict.hevy.noSuggestions,
              gapsTitle: dict.hevy.gapsTitle,
              gapsLead: dict.hevy.gapsLead,
              coverage:
                suggested.explainedExercises + suggested.unexplainedExercises > 0
                  ? dict.hevy.catalogueCoverage(
                      suggested.explainedExercises,
                      suggested.explainedExercises + suggested.unexplainedExercises,
                    )
                  : null,
              moreGaps:
                suggested.totalCatalogueGaps > suggested.catalogueGaps.length
                  ? dict.hevy.moreGaps(
                      suggested.totalCatalogueGaps - suggested.catalogueGaps.length,
                    )
                  : null,
              impliedBy: dict.hevy.impliedBy,
              // The vocabulary is translated by the same dictionary the checkbox list uses, so
              // an item never reads one way here and another way there.
              itemLabel: (item) => strings.items[item as keyof typeof strings.items] ?? item,
            }}
          />
        )}
      </div>
    </>
  );
}
