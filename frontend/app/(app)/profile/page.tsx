import { cookies } from "next/headers";

import { PageHeader } from "@/components/ui/page-header";
import { API_URL } from "@/lib/api";
import { secondsToMinutes } from "@/lib/duration";
import { getDictionary } from "@/lib/i18n";
import { ProfileForm, type GoalChoice } from "./profile-form";

/**
 * The bounds the backend validates against, mirrored here only as form affordances — the
 * `min`/`max` on a number input. They are not the authority: TD-002 and TD-012 are, the API
 * enforces them, and a rejection comes back with its own bounds attached.
 */
const DEFAULTS = { days: 4, minutes: 60 };

type Profile = {
  goal: string;
  daysPerWeek: number;
  sessionDurationSeconds: number;
};

/** Reads the saved profile, or null when there is not one yet. */
async function getProfile(): Promise<Profile | null> {
  const cookieStore = await cookies();
  const response = await fetch(`${API_URL}/training/profile`, {
    headers: { cookie: cookieStore.toString() },
    cache: "no-store",
  });

  if (!response.ok) return null;
  return (await response.json()) as Profile;
}

export default async function ProfilePage() {
  const [dict, profile] = await Promise.all([getDictionary(), getProfile()]);

  // Collected by the schema, programmed one at a time. Surfacing the rest as unavailable is
  // what ADR-004 asks for: the field is right from the first migration, and nothing is
  // generated for a goal no decision record covers.
  const goals: GoalChoice[] = [
    { value: "Hypertrophy", label: dict.profile.goalHypertrophy, available: true },
    { value: "Strength", label: dict.profile.goalStrength, available: false },
    { value: "WeightLoss", label: dict.profile.goalWeightLoss, available: false },
    { value: "Endurance", label: dict.profile.goalEndurance, available: false },
  ];

  return (
    <>
      <PageHeader title={dict.profile.title} lead={dict.profile.lead} />
      <ProfileForm
        goals={goals}
        currentGoal={profile?.goal ?? "Hypertrophy"}
        currentDays={profile?.daysPerWeek ?? DEFAULTS.days}
        // Seconds arrive canonical and become minutes here, at the render edge (root standard 4).
        currentMinutes={
          profile ? secondsToMinutes(profile.sessionDurationSeconds) : DEFAULTS.minutes
        }
        bounds={{ minDays: 2, maxDays: 6, minMinutes: 25, maxMinutes: 120 }}
        strings={{
          goalLabel: dict.profile.goalLabel,
          goalHint: dict.profile.goalHint,
          unavailable: dict.profile.unavailable,
          daysLabel: dict.profile.daysLabel,
          daysHint: dict.profile.daysHint,
          durationLabel: dict.profile.durationLabel,
          durationHint: dict.profile.durationHint,
          save: dict.profile.save,
          saving: dict.profile.saving,
          saved: dict.profile.saved,
        }}
      />
    </>
  );
}
