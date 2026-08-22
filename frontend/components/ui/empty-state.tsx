/**
 * This product has nothing to show until a training history is imported, so the empty state
 * is a screen people will actually live with. It says what will land here, not just that
 * nothing has.
 */
export function EmptyState({ title, body }: { title: string; body: string }) {
  return (
    <div className="rounded-lg border border-dashed border-line-strong bg-surface/50 px-6 py-14 text-center">
      <p className="text-sm font-medium text-ink">{title}</p>
      <p className="mx-auto mt-1.5 max-w-sm text-sm text-ink-muted">{body}</p>
    </div>
  );
}
