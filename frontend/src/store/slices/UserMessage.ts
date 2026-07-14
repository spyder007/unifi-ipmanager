import { createSlice, PayloadAction } from "@reduxjs/toolkit";

export interface UserMessage {
  messageType: "error" | "info" | "warning" | "success";
  message: string;
}

export interface UserMessageSliceState {
  messages: UserMessage[];
}

const initialState: UserMessageSliceState = { messages: [] };

const userMessageSlice = createSlice({
  name: "userMessage",
  initialState,
  reducers: {
    addMessage: (state, action: PayloadAction<UserMessage>): void => {
      const maybeExistingMessage = state.messages.find(
        ({ messageType, message }) =>
          message.indexOf(action.payload.message) === 0 &&
          action.payload.messageType === messageType,
      );

      // only add more error messages if the message isn't already displayed.
      if (!maybeExistingMessage) {
        state.messages.push(action.payload);
      }
    },
    removeMessage: (state, action: PayloadAction<UserMessage>): void => {
      const index = state.messages.findIndex(
        ({ messageType, message }) =>
          message.indexOf(action.payload.message) === 0 &&
          action.payload.messageType === messageType,
      );
      if (index > -1) {
        state.messages.splice(index, 1);
      }
    },
    clearMessages: (state): void => {
      state.messages = [];
    },
    showMessage: (state, action: PayloadAction<UserMessage>): void => {
      state.messages = [action.payload];
    },
  },
});

export const { addMessage, clearMessages, showMessage, removeMessage } =
  userMessageSlice.actions;

export const userMessageReducer = userMessageSlice.reducer;
