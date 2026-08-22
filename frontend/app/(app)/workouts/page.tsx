import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { getDictionary } from "@/lib/i18n";

export default async function WorkoutsPage() {
  const dict = await getDictionary();

  return (
    <>
      <PageHeader title={dict.workouts.title} lead={dict.workouts.lead} />
      <EmptyState title={dict.workouts.emptyTitle} body={dict.workouts.emptyBody} />
    </>
  );
}
