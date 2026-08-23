import { Button } from "@/components/ui/button";
import { Card, CardHeader } from "@/components/ui/card";
import { answerSuggestion } from "./suggestion-actions";

export type Suggestion = {
  item: string;
  impliedByTitle: string;
  impliedByExternalTemplateId: string;
  lastTrainedAt: string;
};

export type Gap = {
  externalTemplateId: string;
  title: string | null;
  lastTrainedAt: string;
};

export type SuggestionStrings = {
  title: string;
  lead: string;
  accept: string;
  decline: string;
  empty: string;
  gapsTitle: string;
  gapsLead: string;
  /** Already interpolated per item — the dictionary owns the sentence, this owns the layout. */
  impliedBy: (title: string) => string;
  itemLabel: (item: string) => string;
};

/**
 * Equipment the history implies, and movements the catalogue does not know.
 *
 * Every suggestion cites the exercise that implied it and when: a suggestion the user cannot
 * audit is an assertion (ADR-020).
 */
export function Suggestions({
  suggestions,
  gaps,
  strings,
}: {
  suggestions: Suggestion[];
  gaps: Gap[];
  strings: SuggestionStrings;
}) {
  return (
    <>
      <Card>
        <div data-testid="equipment-suggestions">
          <CardHeader title={strings.title} />
          <p className="mb-3 text-xs text-ink-muted">{strings.lead}</p>

          {suggestions.length === 0 ? (
            <p className="text-sm text-ink-muted" data-testid="suggestions-empty">
              {strings.empty}
            </p>
          ) : (
            <ul className="flex flex-col divide-y divide-line">
              {suggestions.map((suggestion) => (
                <li
                  key={suggestion.item}
                  data-testid={`suggestion-${suggestion.item}`}
                  className="flex flex-wrap items-center justify-between gap-3 py-2.5 first:pt-0 last:pb-0"
                >
                  <span className="flex flex-col">
                    <span className="text-sm text-ink">{strings.itemLabel(suggestion.item)}</span>
                    <span className="text-xs text-ink-muted">
                      {strings.impliedBy(suggestion.impliedByTitle)}
                    </span>
                  </span>

                  <span className="flex items-center gap-2">
                    <form action={answerSuggestion}>
                      <input type="hidden" name="item" value={suggestion.item} />
                      <input type="hidden" name="accepted" value="true" />
                      <Button type="submit" size="sm" data-testid={`accept-${suggestion.item}`}>
                        {strings.accept}
                      </Button>
                    </form>
                    <form action={answerSuggestion}>
                      <input type="hidden" name="item" value={suggestion.item} />
                      <input type="hidden" name="accepted" value="false" />
                      <Button
                        type="submit"
                        variant="ghost"
                        size="sm"
                        data-testid={`decline-${suggestion.item}`}
                      >
                        {strings.decline}
                      </Button>
                    </form>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </Card>

      {gaps.length > 0 && (
        <Card>
          <div data-testid="catalogue-gaps">
            <CardHeader title={strings.gapsTitle} />
            <p className="mb-3 text-xs text-ink-muted">{strings.gapsLead}</p>
            <ul className="flex flex-col divide-y divide-line">
              {gaps.map((gap) => (
                <li key={gap.externalTemplateId} className="py-2 first:pt-0 last:pb-0">
                  <span className="text-sm text-ink">{gap.title ?? gap.externalTemplateId}</span>
                  <span className="tabular ml-2 font-mono text-xs text-ink-muted">
                    {gap.externalTemplateId}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </Card>
      )}
    </>
  );
}
