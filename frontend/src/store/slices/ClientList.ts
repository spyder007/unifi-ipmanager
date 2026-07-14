import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import * as api from "../../api/api";
import ApiWrapper, { checkCommunicationError } from "../../utils/apiWrapper";
import { AppThunk } from "../store";
import { addMessage, clearMessages } from "./UserMessage";

export interface ClientListState {
  items: api.UniClient[];
  isLoading: boolean;
  filter?: string;
  expandedObjectIds: string[];
  totalItems: number;
}

interface FetchClientResult {
  filter: string | undefined;
  data: api.ServiceResultOfListOfUniClient;
}

const initialState: ClientListState = {
  items: [],
  isLoading: false,
  filter: undefined,
  expandedObjectIds: [],
  totalItems: 0,
};

function startLoading(state: ClientListState): void {
  state.isLoading = true;
}

function stopLoading(state: ClientListState): void {
  state.isLoading = false;
}

const clientListSlice = createSlice({
  name: "clientList",
  initialState,
  reducers: {
    getClientListStart: startLoading,
    getClientListCompleted: stopLoading,
    getClientList: (state, action: PayloadAction<FetchClientResult>): void => {
      state.filter = action.payload.filter;
      state.items = action.payload.data.data ?? [];
      state.totalItems = state.items.length;
      state.isLoading = false;
    },
    toggleExpand: (state, action: PayloadAction<string>): void => {
      if (!state.expandedObjectIds) {
        state.expandedObjectIds = [];
      }

      if (state.expandedObjectIds.length === 0) {
        state.expandedObjectIds.push(action.payload);
      } else {
        const currentIndex = state.expandedObjectIds.findIndex(
          (element) => element === action.payload,
        );
        if (currentIndex === -1) {
          state.expandedObjectIds.push(action.payload);
        } else {
          state.expandedObjectIds.splice(currentIndex, 1);
        }
      }
    },
  },
});

export const {
  getClientListStart,
  getClientListCompleted,
  getClientList,
  toggleExpand,
} = clientListSlice.actions;

export const deleteClient =
  (mac: string, filter?: string): AppThunk =>
  async (dispatch): Promise<void> => {
    dispatch(clearMessages());
    const apiWrapper: ApiWrapper = ApiWrapper.getInstance();

    const result = await apiWrapper.callWithToken((headers, baseUrl) =>
      api.ClientApiFactory(undefined, baseUrl).clientDeleteClient(mac, headers),
    );
    const error = checkCommunicationError(result);
    if (error) {
      dispatch(
        addMessage({
          messageType: "error",
          message: error,
        }),
      );
      dispatch(getClientListCompleted());
    } else {
      dispatch(fetchClientList(filter));
    }
  };

export const fetchClientList =
  (filter?: string): AppThunk =>
  async (dispatch): Promise<void> => {
    dispatch(getClientListStart());
    dispatch(clearMessages());
    const apiWrapper: ApiWrapper = ApiWrapper.getInstance();

    const result = await apiWrapper.callWithToken((headers, baseUrl) =>
      api.ClientApiFactory(undefined, baseUrl).clientGet(headers),
    );
    const error = checkCommunicationError(result);
    if (error) {
      dispatch(
        addMessage({
          messageType: "error",
          message: error,
        }),
      );
      dispatch(getClientListCompleted());
    } else {
      dispatch(
        getClientList({
          data: result.data,
          filter,
        }),
      );
    }
  };

export const refreshWorkList =
  (): AppThunk =>
  async (dispatch, getState): Promise<void> => {
    dispatch(fetchClientList(getState().clientList.filter));
  };

export const clientListReducer = clientListSlice.reducer;
