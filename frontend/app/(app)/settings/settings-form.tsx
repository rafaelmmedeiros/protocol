"use client";

import { useActionState } from "react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { updatePreferences, type PreferencesState } from "./actions";

export type Choice = { value: string; label: string; testId: string };

export type SettingsStrings = {
  appearance: string;
  themeLabel: string;
  themeDescription: string;
  languageLabel: string;
  languageDescription: string;
  save: string;
  saving: string;
  saved: string;
};

export function SettingsForm({
  themes,
  locales,
  currentTheme,
  currentLocale,
  strings,
}: {
  themes: Choice[];
  locales: Choice[];
  currentTheme: string;
  currentLocale: string;
  strings: SettingsStrings;
}) {
  const [state, action] = useActionState<PreferencesState, FormData>(updatePreferences, {
    saved: false,
  });

  return (
    <form action={action} className="flex flex-col gap-8">
      <ChoiceGroup
        name="theme"
        legend={strings.themeLabel}
        description={strings.themeDescription}
        choices={themes}
        defaultValue={currentTheme}
      />
      <ChoiceGroup
        name="locale"
        legend={strings.languageLabel}
        description={strings.languageDescription}
        choices={locales}
        defaultValue={currentLocale}
      />

      <div className="flex items-center gap-3">
        <SubmitButton save={strings.save} saving={strings.saving} />
        {state.saved && (
          <p data-testid="preferences-saved" role="status" className="text-sm text-ok">
            {strings.saved}
          </p>
        )}
      </div>
    </form>
  );
}

function SubmitButton({ save, saving }: { save: string; saving: string }) {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" data-testid="save-preferences" disabled={pending}>
      {pending ? saving : save}
    </Button>
  );
}

/**
 * A segmented control that is really a radio group: real `<input type="radio">` elements with
 * real labels, so arrow keys move between options and a screen reader announces the group.
 * The styling hangs off `peer-checked`, never off JavaScript state.
 */
function ChoiceGroup({
  name,
  legend,
  description,
  choices,
  defaultValue,
}: {
  name: string;
  legend: string;
  description: string;
  choices: Choice[];
  defaultValue: string;
}) {
  return (
    <fieldset>
      <legend className="text-sm font-medium text-ink">{legend}</legend>
      <p className="mt-1 mb-3 text-sm text-ink-muted">{description}</p>

      <div className="inline-flex flex-wrap gap-1 rounded-lg border border-line bg-surface p-1">
        {choices.map((choice) => (
          <div key={choice.value}>
            <input
              type="radio"
              id={`${name}-${choice.value}`}
              name={name}
              value={choice.value}
              defaultChecked={choice.value === defaultValue}
              className="peer sr-only"
            />
            {/* The test id is on the label, not the input: the input is visually hidden, so
                the label is both what a person clicks and what Playwright can click. */}
            <label
              htmlFor={`${name}-${choice.value}`}
              data-testid={choice.testId}
              className={cn(
                "block cursor-pointer rounded-md px-4 py-1.5 text-sm text-ink-muted transition-colors",
                "hover:bg-surface-muted hover:text-ink",
                "peer-checked:bg-accent-fill peer-checked:text-on-accent peer-checked:hover:bg-accent-fill-hover",
                "peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-accent",
              )}
            >
              {choice.label}
            </label>
          </div>
        ))}
      </div>
    </fieldset>
  );
}
