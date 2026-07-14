import { faBars } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { AuthenticationMenu } from "../AuthenticationMenu/AuthenticationMenu";
import React from "react";
import { Link } from "react-router-dom";
import "./NavMenu.scss";

export interface NavigationBarProperties {
  brand?: string;
  children?: React.ReactNode;
}

export const NavMenu: React.FunctionComponent<NavigationBarProperties> = (
  props: NavigationBarProperties
) => {
  return (
    <nav className="navbar navbar-light navbar-expand-lg bg-white border-bottom border-primary shadow-sm">
      <div className="container-fluid">
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navMenuCollapse"
          aria-controls="navMenuCollapse"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <FontAwesomeIcon icon={faBars} />
        </button>
        <Link className="navbar-brand text-primary d-none d-lg-block" to="/">
          Unifi Client
        </Link>
        <Link className="navbar-brand text-primary d-lg-none" to="/">
          UC
        </Link>

        <div id="navMenuCollapse" className="collapse navbar-collapse">
          <ul className="navbar-nav navbar-light me-auto mb-2 mb-lg-0">
            <li className="nav-item">
              <Link className="nav-link text-primary" to="/clients">
                Clients
              </Link>
            </li>
          </ul>
        </div>
        <AuthenticationMenu />
      </div>
    </nav>
  );
};
