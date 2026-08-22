import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { getDictionary } from "@/lib/i18n";

export default async function AdjustmentsPage() {
  const dict = await getDictionary();

  return (
    <>
      <PageHeader title={dict.adjustments.title} lead={dict.adjustments.lead} />
      <EmptyState title={dict.adjustments.emptyTitle} body={dict.adjustments.emptyBody} />
    </>
  );
}
