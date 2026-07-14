import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { AccountInfo } from "@azure/msal-common";

export interface UserState {
  user?: AccountInfo;
}

interface SetUserPayload {
  user?: AccountInfo;
}

const initialState: UserState = { user: undefined };

const userSlice = createSlice({
  name: "user",
  initialState,
  reducers: {
    setUser: (state, action: PayloadAction<SetUserPayload>): void => {
      state.user = action.payload.user;
    },
  },
});

export const { setUser } = userSlice.actions;

export const userSliceReducer = userSlice.reducer;
