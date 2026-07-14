import {
  PublicClientApplication,
  AuthenticationResult,
  AccountInfo,
  EndSessionRequest,
  RedirectRequest,
  PopupRequest,
} from "@azure/msal-browser";

import { MSAL_CONFIG } from "./authConfig";

// Add here scopes for id token to be used at MS Identity Platform endpoints.
export const applicationScopes: string[] = [
  "openid",
  "profile",
  "unifi.ipmanager",
];

export class AzureAuthenticationContext {
  private static instance: AzureAuthenticationContext;

  public static getInstance(): AzureAuthenticationContext {
    if (!AzureAuthenticationContext.instance) {
      AzureAuthenticationContext.instance = new AzureAuthenticationContext();
    }

    return AzureAuthenticationContext.instance;
  }

  readonly pcaInstance: PublicClientApplication = new PublicClientApplication(
    MSAL_CONFIG
  );

  private account?: AccountInfo;
  private loginRedirectRequest?: RedirectRequest;
  private loginRequest?: PopupRequest;

  public isAuthenticationConfigured = false;

  private constructor() {
    // @ts-ignore
    this.account = null;
    this.setRequestObjects();
    if (MSAL_CONFIG?.auth?.clientId) {
      this.isAuthenticationConfigured = true;
    }
  }

  login(signInType: string, setUser: any): void {
    if (signInType === "loginPopup") {
      this.pcaInstance
        .loginPopup(this.loginRequest)
        .then((resp: AuthenticationResult) => {
          this.handleResponse(resp, setUser);
        })
        .catch((err) => {
          console.error(err);
        });
    } else if (signInType === "loginRedirect") {
      this.pcaInstance.loginRedirect(this.loginRedirectRequest);
    }
  }

  logout(account: AccountInfo): void {
    const logOutRequest: EndSessionRequest = {
      account,
    };

    this.pcaInstance.logout(logOutRequest);
  }
  handleResponse(response: AuthenticationResult, incomingFunction: any) {
    if (response !== null && response.account !== null) {
      this.account = response.account;
    } else {
      this.account = this.getAccount();
    }

    if (this.account) {
      incomingFunction(this.account);
    }
  }

  private setRequestObjects(): void {
    this.loginRequest = {
      scopes: applicationScopes,
      prompt: "select_account",
    };

    this.loginRedirectRequest = {
      ...this.loginRequest,
      redirectStartPage: window.location.href,
    };
  }

  private getAccount(): AccountInfo | undefined {
    console.log(`loadAuthModule`);
    const currentAccounts = this.pcaInstance.getAllAccounts();
    if (currentAccounts === null) {
      // @ts-ignore
      console.log("No accounts detected");
      return undefined;
    }

    if (currentAccounts.length > 1) {
      // TBD: Add choose account code here
      // @ts-ignore
      console.log(
        "Multiple accounts detected, need to add choose account code."
      );
      return currentAccounts[0];
    } else if (currentAccounts.length === 1) {
      return currentAccounts[0];
    }
  }
}

export default AzureAuthenticationContext;
