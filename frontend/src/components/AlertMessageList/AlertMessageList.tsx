import React from "react";
import { useSelector } from "react-redux";
import { RootState } from "../../store/rootReducer";
import { UserMessage } from "../../store/slices/UserMessage";
import { AlertMessage } from "../AlertMessage/AlertMessage";
import "./AlertMessageList.scss";

export const AlertMessageList = (): JSX.Element | null => {
  const { messages } = useSelector((state: RootState) => state.userMessage);

  if (!messages) {
    return null;
  }

  return (
    <div className="container mainsite-alerts-container">
      {messages.map((message: UserMessage) => (
        <AlertMessage key={message.message} {...message} />
      ))}
    </div>
  );
};
