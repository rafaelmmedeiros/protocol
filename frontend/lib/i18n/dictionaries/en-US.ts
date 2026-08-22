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
    adjustments: "Adjustments",
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
  adjustments: {
    title: "Adjustments",
    lead: "What this system proposes changing, and why.",
    emptyTitle: "Nothing to adjust yet",
    emptyBody: "Adjustments need a training history to reason about. Import one first.",
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
    typographyLead: "One system stack, because a webfont needs the network at build time and makes the Docker build flaky. Numbers that line up in columns get tabular figures.",
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
