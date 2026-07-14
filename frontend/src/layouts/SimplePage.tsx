import {
  AuthenticatedTemplate,
  UnauthenticatedTemplate,
} from "@azure/msal-react";
import React from "react";
import { Outlet, Navigate } from "react-router-dom";
import { AlertMessageList } from "../components/AlertMessageList/AlertMessageList";
import { Footer } from "../components/Footer/Footer";
import { NavMenu } from "../components/NavMenu/NavMenu";

interface SimpleProps {
  isPrivate?: boolean;
}

export const SimplePage: React.FC<SimpleProps> = (props) => {
  const layout = (
    <React.Fragment>
      <NavMenu />
      <div className="container">
        <AlertMessageList />
        <Outlet />
      </div>
      <Footer />
    </React.Fragment>
  );

  if (props.isPrivate) {
    return (
      <>
        <AuthenticatedTemplate>{layout}</AuthenticatedTemplate>
        <UnauthenticatedTemplate>
          <Navigate to={"/"} />
        </UnauthenticatedTemplate>
      </>
    );
  } else {
    return layout;
  }
};
