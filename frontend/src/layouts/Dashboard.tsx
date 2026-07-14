export {};
// import React from 'react';
// import { useSelector } from 'react-redux';
// import { Navigate, Route } from "react-router-dom";
// import { AuthenticatedTemplate, UnauthenticatedTemplate } from "@azure/msal-react";
// import { AlertMessageList } from '../components/AlertMessageList/AlertMessageList';
// import { Footer } from '../components/Footer/Footer';
// import { NavMenu } from '../components/NavMenu/NavMenu';

// interface DashboardProps {
//     component: any
//     path?: string;
//     exact?: boolean;
//     isPrivate?: boolean;
// }

// export const DashboardPage: React.FC<DashboardProps> = (props) => {
//     const { component: Component, isPrivate: boolean, ...rest } = props;

//     const layoutRender = (matchProps: any) => (
//         <React.Fragment>
//             <NavMenu />
//             <div className="container">
//                 <AlertMessageList />
//                 <Component {...matchProps} />
//             </div>
//             <Footer />
//         </React.Fragment>
//     );

//     const renderRoute = () => {
//         if (props.isPrivate) {
//             return (
//                 <>
//                     <AuthenticatedTemplate>
//                         <Route {...rest} component={layoutRender} />
//                     </AuthenticatedTemplate>
//                     <UnauthenticatedTemplate>
//                         <Navigate to={'/'} />
//                     </UnauthenticatedTemplate>
//                 </>
//             )

//         }

//         return (<Route {...rest} render={layoutRender} />);
//     };

//     return renderRoute();
// };
