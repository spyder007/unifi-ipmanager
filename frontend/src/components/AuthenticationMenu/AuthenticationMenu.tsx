import React from "react";
import AzureAuthenticationContext from "../../utils/authProvider";
import { AccountInfo } from "@azure/msal-browser";
import {
  AuthenticatedTemplate,
  UnauthenticatedTemplate,
} from "@azure/msal-react";
import { useDispatch, useSelector } from "react-redux";
import { RootState } from "../../store/rootReducer";
import { setUser } from "../../store/slices/User";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faMicrosoft } from "@fortawesome/free-brands-svg-icons/faMicrosoft";
import { faUser } from "@fortawesome/free-solid-svg-icons";

const ua = window.navigator.userAgent;
const msie = ua.indexOf("MSIE ");
const msie11 = ua.indexOf("Trident/");
const isIE = msie > 0 || msie11 > 0;

// Log In, Log Out button
export const AuthenticationMenu = (): JSX.Element => {
  // Azure client context
  const authenticationModule: AzureAuthenticationContext =
    AzureAuthenticationContext.getInstance();

  const dispatch = useDispatch();

  const { user } = useSelector((state: RootState) => state.user);

  const logIn = (method: string): any => {
    const typeName = "loginPopup";
    const logInType = isIE ? "loginRedirect" : typeName;

    // Azure Login
    authenticationModule.login(logInType, returnedAccountInfo);
  };
  const logOut = (): any => {
    if (user) {
      dispatch(setUser({ user: undefined }));
      // Azure Logout
      authenticationModule.logout(user);
    }
  };

  const returnedAccountInfo = (user: AccountInfo) => {
    // set state
    dispatch(setUser({ user: user }));
  };

  return (
    <ul className="navbar-nav">
      <li className="nav-item dropdown">
        <button
          className="nav-link dropdown-toggle btn"
          id="navbarDropdown"
          data-bs-toggle="dropdown"
          aria-expanded="false"
        >
          <AuthenticatedTemplate>
            <FontAwesomeIcon icon={faUser}></FontAwesomeIcon>
          </AuthenticatedTemplate>
          <UnauthenticatedTemplate>
            <FontAwesomeIcon icon={faMicrosoft}></FontAwesomeIcon>
          </UnauthenticatedTemplate>
        </button>
        <ul
          className="dropdown-menu dropdown-menu-end"
          aria-labelledby="navbarDropdown"
        >
          <AuthenticatedTemplate>
            <li>
              <span className="dropdown-item">{user?.username}</span>
            </li>
            <li>
              <button className="dropdown-item" onClick={() => logOut()}>
                Log out
              </button>
            </li>
          </AuthenticatedTemplate>
          <UnauthenticatedTemplate>
            <li>
              <button
                className="dropdown-item"
                onClick={() => logIn("loginPopup")}
              >
                Log in
              </button>
            </li>
          </UnauthenticatedTemplate>
        </ul>
      </li>
    </ul>
  );
};
