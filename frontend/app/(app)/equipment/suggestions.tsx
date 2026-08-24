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
  timesTrained: number;
};

export type SuggestionStrings = {
  title: string;
  lead: string;
  accept: string;
  decline: string;
  empty: string;
  gapsTitle: string;
  gapsLead: string;
  /**
   * Already interpolated: how much of the logged training the catalogue explains.
   *
   * Null when nothing has been imported yet -- "0 of 0" reads as a failure when it is an
   * absence, and the suggestions card already says there is nothing to work from.
   */
  coverage: string | null;
  /** Already interpolated: how many gaps exist beyond the ones named. */
  moreGaps: string | null;
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
  /** The most-trained few, not all of them -- a real account had 3,798. */
  gaps: Gap[];
  strings: SuggestionStrings;
}) {
  // The proportion belongs above the list rather than inside it: twenty names read the same
  // whether they cover 3% of someone's training or 73%, and this is the line that tells them
  // apart. It is shown even when there are no gaps left, because "we recognise all of it" is the
  // outcome the milestone is aiming at and an absent line cannot say so.
  return (
    <>
      <Card>
        <div data-testid="equipment-suggestions">
          <CardHeader title={strings.title} />
          <p className="mb-3 text-xs text-ink-muted">{strings.lead}</p>

          {suggestions.length === 0 ? (
            <p
              className="text-sm text-ink-muted"
              data-testid="suggestions-empty"
            >
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
                    <span className="text-sm text-ink">
                      {strings.itemLabel(suggestion.item)}
                    </span>
                    <span className="text-xs text-ink-muted">
                      {strings.impliedBy(suggestion.impliedByTitle)}
                    </span>
                  </span>

                  <span className="flex items-center gap-2">
                    <form action={answerSuggestion}>
                      <input
                        type="hidden"
                        name="item"
                        value={suggestion.item}
                      />
                      <input type="hidden" name="accepted" value="true" />
                      <Button
                        type="submit"
                        size="sm"
                        data-testid={`accept-${suggestion.item}`}
                      >
                        {strings.accept}
                      </Button>
                    </form>
                    <form action={answerSuggestion}>
                      <input
                        type="hidden"
                        name="item"
                        value={suggestion.item}
                      />
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

      {(strings.coverage || gaps.length > 0) && (
        <Card>
          <div data-testid="catalogue-gaps">
            <CardHeader title={strings.gapsTitle} />
            <p className="mb-3 text-xs text-ink-muted">{strings.gapsLead}</p>

            {strings.coverage && (
              <p
                className="mb-3 text-sm text-ink"
                data-testid="catalogue-coverage"
              >
                {strings.coverage}
              </p>
            )}

            {gaps.length > 0 && (
              <ul
                className="flex flex-col divide-y divide-line"
                data-testid="catalogue-gap-list"
              >
                {gaps.map((gap) => (
                  <li
                    key={gap.externalTemplateId}
                    className="flex items-baseline justify-between gap-3 py-2 first:pt-0 last:pb-0"
                  >
                    <span className="text-sm text-ink">
                      {gap.title ?? gap.externalTemplateId}
                    </span>
                    <span className="tabular font-mono text-xs text-ink-muted">
                      &times;{gap.timesTrained}
                    </span>
                  </li>
                ))}
              </ul>
            )}
            {strings.moreGaps && (
              <p
                className="mt-3 text-xs text-ink-muted"
                data-testid="catalogue-gaps-more"
              >
                {strings.moreGaps}
              </p>
            )}
          </div>
        </Card>
      )}
    </>
  );
}
