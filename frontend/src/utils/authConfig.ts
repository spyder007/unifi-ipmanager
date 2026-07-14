import { Configuration, LogLevel } from "@azure/msal-browser";
import { ProtocolMode } from "@azure/msal-common";
import { getAuthConfig } from "../components/Config";

export const MSAL_CONFIG: Configuration = {
  auth: {
    authority: getAuthConfig("authority"),
    clientId: getAuthConfig("clientId"),
    knownAuthorities: [getAuthConfig("authority")],
    redirectUri: getAuthConfig("redirectUri"),
    protocolMode: ProtocolMode.OIDC,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          case LogLevel.Info:
            console.info(message);
            return;
          case LogLevel.Verbose:
            console.debug(message);
            return;
          case LogLevel.Warning:
            console.warn(message);
            return;
        }
      },
    },
  },
};
