import { AxiosPromise, AxiosResponse } from "axios";
import { AzureAuthenticationContext } from "./authProvider";
import { AuthenticationResult, SilentRequest } from "@azure/msal-browser";
import { getApiConfig } from "../components/Config";
import { applicationScopes } from "./authProvider";

class ApiWrapper {
  private static instance: ApiWrapper;

  private constructor() {}

  private accessToken?: string;

  public static getInstance(): ApiWrapper {
    if (!ApiWrapper.instance) {
      ApiWrapper.instance = new ApiWrapper();
    }

    return ApiWrapper.instance;
  }

  private buildHeaders(token: string | null) {
    if (!token) {
      return {
        headers: {},
      };
    } else {
      return {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      };
    }
  }

  public callWithToken<T>(
    fn: (headers: any, baseUrl: string) => AxiosPromise<T>
  ): AxiosPromise<T> {
    const baseUrl = getApiConfig("backend") || "http://localhost:5000";
    if (!this.accessToken) {
      const accounts =
        AzureAuthenticationContext.getInstance().pcaInstance.getAllAccounts();

      if (accounts.length > 0) {
        const request: SilentRequest = {
          scopes: applicationScopes,
          account: accounts[0],
        };

        return AzureAuthenticationContext.getInstance()
          .pcaInstance.acquireTokenSilent(request)
          .then((authResult: AuthenticationResult) => {
            this.accessToken = authResult.accessToken;
            return fn(this.buildHeaders(this.accessToken || ""), baseUrl);
          });
      }
    }

    return fn(this.buildHeaders(this.accessToken || ""), baseUrl);
  }
}

export default ApiWrapper;

export function checkCommunicationError<T>(
  response: AxiosResponse<T>
): string | undefined {
  if (response.status < 200 || response.status >= 300) {
    return response.statusText;
  }
  return undefined;
}
