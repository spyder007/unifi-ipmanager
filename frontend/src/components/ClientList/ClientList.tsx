import React, { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { useAppDispatch } from "../../store/store";
import { RootState } from "../../store/rootReducer";
import { deleteClient, fetchClientList } from "../../store/slices/ClientList";
import { AgGridReact } from "ag-grid-react";
import { faTrashAlt } from "@fortawesome/free-solid-svg-icons/faTrashAlt";
import { IconCellButtonRenderer } from "../CellRenderers/IconCellButtonRenderer";
import { UniClient } from "../../api";
import "./ClientList.scss";
import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-alpine.css";
import {
  ColDef,
  ColGroupDef,
  RowClassParams,
  ValueGetterParams,
} from "ag-grid-community";

export const ClientList: React.FunctionComponent<{}> = (): JSX.Element => {
  const user = useSelector((state: RootState) => state.user.user);
  const dispatch = useAppDispatch();
  const { items } = useSelector((state: RootState) => state.clientList);

  const filter = "";
  const initialSort: "asc" | "desc" | null | undefined = "asc";
  useEffect(() => {
    dispatch(fetchClientList(filter));
  }, [user, dispatch, filter]);

  const compareIpAddresses = (ipA: string, ipB: string): number => {
    const numA = Number(
      ipA
        .split(".")
        .map((num, idx) => parseInt(num) * Math.pow(2, (3 - idx) * 8))
         
        .reduce((a, v) => ((a += v), a), 0)
    );

    const numB = Number(
      ipB
        .split(".")
        .map((num, idx) => parseInt(num) * Math.pow(2, (3 - idx) * 8))
         
        .reduce((a, v) => ((a += v), a), 0)
    );

    return numA - numB;
  };

  const [columnDefs] = useState<
    (ColDef<UniClient, any> | ColGroupDef<UniClient>)[] | null | undefined
  >([
    { field: "name", sortable: true },
    { field: "ipGroup", headerName: "Group" },
    {
      field: "fixed_ip",
      headerName: "IP",
      sortable: true,
      initialSort,
      comparator: (valueA: string, valueB: string): number => {
        return compareIpAddresses(valueA, valueB);
      },
    },
    { field: "mac" },
    { field: "objectType", headerName: "Type" },
    {
      headerName: "IP on Device?",
      valueGetter: (params: ValueGetterParams): string => {
        return params.data.notes?.set_on_device ? "true" : "false";
      },
      // cellRender: (params: any): any => {
      //     if (params.value) {
      //         return (<div>true</div>);
      //     }
      //     return (<div>false</div>);
      // }
    },
    {
      headerName: "",
      width: 50,
      field: "mac",
      cellRenderer: IconCellButtonRenderer,
      cellRendererParams: {
        clicked: (mac: string): void => {
          dispatch(deleteClient(mac, filter));
        },
        icon: faTrashAlt,
      },
    },
  ]);

  const getRowClass = (params: RowClassParams): string => {
    return `ip-group-${params.data.ipGroup}`;
  };

  return (
    <div className="ag-theme-alpine" style={{ height: "calc(100vh - 150px)" }}>
      <div style={{ height: "100%", width: "100%" }}>
        <AgGridReact<UniClient>
          rowData={items}
          columnDefs={columnDefs}
          getRowClass={getRowClass}
        ></AgGridReact>
      </div>
    </div>
  );
};
