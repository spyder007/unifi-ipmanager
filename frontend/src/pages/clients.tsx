import React from "react";
import { ClientList } from "../components/ClientList/ClientList";

export const Clients: React.FunctionComponent = () => {
  return (
    <div className="container">
      <ClientList />
    </div>
  );
};
