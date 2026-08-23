/**
 * The source of truth for every user-visible string, and for the shape of a dictionary.
 * `Dictionary` is derived from this object, so a key added here fails to compile in every
 * other locale until it is translated there too.
 */
export const enUS = {
  app: {
    name: "Protocol",
    tagline: "Training intelligence",
  },
  nav: {
    primary: "Main navigation",
    skipToContent: "Skip to content",
    dashboard: "Dashboard",
    workouts: "Workouts",
    profile: "Profile",
    equipment: "Equipment",
    template: "Template",
  },
  user: {
    menu: "Account menu",
    signedInAs: "Signed in as",
    settings: "Settings",
    signOut: "Sign out",
    signingOut: "Signing out...",
  },
  theme: {
    label: "Theme",
    description: "Follows your operating system unless you choose otherwise.",
    system: "System",
    light: "Light",
    dark: "Dark",
  },
  language: {
    label: "Language",
    description: "Applies to every screen. Training data is stored the same way either way.",
  },
  login: {
    signInTitle: "Sign in",
    registerTitle: "Create an account",
    lead: "Training intelligence for what Hevy logs.",
    email: "Email",
    password: "Password",
    signIn: "Sign in",
    register: "Create account",
    working: "Working...",
    needAccount: "Create an account",
    haveAccount: "I already have an account",
    invalidCredentials: "Email or password is incorrect.",
    registerFailed: "Could not create the account.",
  },
  /**
   * Keyed by the error codes ASP.NET Core Identity puts in its ProblemDetails `errors` map.
   * The backend's own English sentence is never shown -- the code is the contract.
   */
  authErrors: {
    DuplicateUserName: "That email already has an account.",
    DuplicateEmail: "That email already has an account.",
    InvalidEmail: "That email address is not valid.",
    PasswordTooShort: "The password is too short.",
    PasswordRequiresDigit: "The password needs at least one digit.",
    PasswordRequiresUpper: "The password needs at least one capital letter.",
    PasswordRequiresLower: "The password needs at least one lowercase letter.",
    PasswordRequiresNonAlphanumeric: "The password needs at least one symbol.",
    PasswordRequiresUniqueChars: "The password needs more distinct characters.",
  },
  dashboard: {
    title: "Dashboard",
    lead: "What the last weeks of training add up to.",
    weeklySets: "Sets this week",
    weeklyVolume: "Volume this week",
    lastSession: "Last session",
    session: "Session",
    email: "Email",
    userId: "User id",
  },
  workouts: {
    title: "Workouts",
    lead: "Every session read out of Hevy, newest first.",
    emptyTitle: "No workouts yet",
    emptyBody: "Once the Hevy import lands, every logged session shows up here.",
  },
  profile: {
    title: "Training profile",
    lead: "What you train for, and what you actually have available. Every session generated for you is built from these three answers.",
    goalLabel: "Goal",
    goalHint: "Only hypertrophy is programmed today. The rest are listed so you can see what is coming.",
    goalHypertrophy: "Muscle growth",
    goalStrength: "Strength",
    goalWeightLoss: "Weight loss",
    goalEndurance: "Endurance",
    unavailable: "not yet",
    daysLabel: "Days a week",
    daysHint: "How many sessions you will realistically train, not how many you would like to.",
    durationLabel: "Minutes a session",
    durationHint: "How long a session can last, door to door. Rest between sets is decided for you.",
    save: "Save profile",
    saving: "Saving...",
    saved: "Profile saved",
  },
  /**
   * Keyed by the codes the training endpoints answer with. The backend never sends a sentence
   * (root standard 3), and the two that carry bounds take them as arguments so the numbers
   * TD-002 and TD-012 decided are never copied into this file.
   */
  profileErrors: {
    GoalNotSupported: "That goal is not programmed yet. Choose muscle growth for now.",
    FrequencyOutOfRange: (min: number, max: number) =>
      `Choose between ${min} and ${max} days a week.`,
    DurationOutOfRange: (min: number, max: number) =>
      `A session has to be between ${min} and ${max} minutes.`,
    ProfileNotFound: "No profile saved yet.",
    unknown: "Could not save the profile.",
  },
  equipment: {
    title: "Equipment",
    lead: "The plates, bars and machines you actually train with. What is here decides what a generated session is allowed to ask for.",
    emptyTitle: "No equipment described yet",
    emptyBody: "Nothing can be programmed in loads you cannot make. Describing a gym starts here.",
  },
  settings: {
    title: "Settings",
    lead: "Preferences for this account. They take effect immediately.",
    appearance: "Appearance",
    save: "Save preferences",
    saving: "Saving...",
    saved: "Preferences saved.",
  },
  template: {
    title: "Template",
    lead: "Every decided piece of the interface, on one page. When something here and something in the product disagree, this page is wrong -- fix it in the same commit.",
    colour: "Colour",
    colourLead: "No component names a colour; it names a role. That is what lets the palette change without touching a component.",
    tokenColumn: "Token",
    roleColumn: "Role",
    reserved: "Reserved data ink",
    reservedLead: "Green and red mean progress and regression, everywhere, always. They are never a brand colour and never decoration.",
    typography: "Typography",
    typographyLead: "Archivo, one variable file of 35 kB served from this origin. Line height and letter spacing belong to the size, not to the component. Its figures are proportional by default, so anything that lines up in a column has to ask for tabular ones.",
    buttons: "Buttons",
    buttonsLead: "One primary action per screen. A destructive action is never a filled button -- filled ember and filled red side by side is the one pairing that confuses.",
    fields: "Fields",
    fieldsLead: "Every input has a real label. Placeholder text is not a label.",
    feedback: "State",
    feedbackLead: "State reads at a glance through shape as well as colour, so it survives being printed, screenshotted, or read by someone colourblind.",
    emptyStates: "Empty states",
    emptyStatesLead: "This product will be empty for a while. An empty state says what will land here, not just that nothing has.",
    stack: "What this is built on",
    stackLead: "Decided, and worth not re-deciding by accident.",
    choiceColumn: "Choice",
    whyColumn: "Why",
  },
  common: {
    noDataYet: "No data yet",
    awaitingImport: "Waiting for the first Hevy import.",
    sets: "sets",
    example: "Example",
  },
} ;

export type Dictionary = typeof enUS;
