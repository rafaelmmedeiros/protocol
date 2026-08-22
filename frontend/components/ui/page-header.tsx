export function PageHeader({ title, lead }: { title: string; lead?: string }) {
  return (
    <header className="mb-8">
      <h1 className="text-2xl font-semibold tracking-tight text-ink">{title}</h1>
      {lead && <p className="mt-1.5 max-w-2xl text-sm text-ink-muted">{lead}</p>}
    </header>
  );
}
