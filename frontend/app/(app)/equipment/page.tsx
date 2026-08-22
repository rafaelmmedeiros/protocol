import { EmptyState } from "@/components/ui/empty-state";
import { PageHeader } from "@/components/ui/page-header";
import { getDictionary } from "@/lib/i18n";

export default async function EquipmentPage() {
  const dict = await getDictionary();

  return (
    <>
      <PageHeader title={dict.equipment.title} lead={dict.equipment.lead} />
      <EmptyState title={dict.equipment.emptyTitle} body={dict.equipment.emptyBody} />
    </>
  );
}
