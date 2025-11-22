import React from "react";
import {JwtHeader, JwtPayload} from "../API/ChasmaWebApiClient";
import '../css/InfoTable.css'

/**
 * The properties of the JWT information table.
 */
interface IProps {
    /** The header of the decoded JWT. **/
    header: JwtHeader | undefined;

    /** The payload of the decoded JWT. **/
    payload : JwtPayload | undefined;
}

/**
 * The JwtInfoTable contents and display components.
 * @param props The property information of the table containing the header and payload details
 * @constructor Initializes a new instance of the JwtInfoTable.
 */
const JwtInfoTable: React.FC<IProps> = (props) => {
    if (!props.header || !props.payload) {
        console.error("Cannot show contents of decoded JWT because the header and payload are not both populated.")
        return null;
    }

    return (
        <div>
            <table className="info-table">
                <caption>JWT Header</caption>
                <thead>
                <tr>
                    <th>Algorithm</th>
                    <th>Type</th>
                </tr>
                </thead>
                <tbody>
                <tr key={props.header.alg}>
                    <td>{props.header.alg}</td>
                    <td>{props.header.typ}</td>
                </tr>
                </tbody>
            </table>
            <br/>
            <table className="info-table">
                <caption>JWT Payload</caption>
                <thead>
                <tr>
                    <th>SUB</th>
                    <th>JTI</th>
                    <th>NBF</th>
                    <th>EXP</th>
                    <th>ISS</th>
                    <th>AUD</th>
                </tr>
                </thead>
                <tbody>
                <tr key={props.payload.sub}>
                    <td>{props.payload.sub}</td>
                    <td>{props.payload.jti}</td>
                    <td>{props.payload.nbf}</td>
                    <td>{props.payload.exp}</td>
                    <td>{props.payload.iss}</td>
                    <td>{props.payload.aud}</td>
                </tr>
                </tbody>
            </table>
        </div>
    );
};

export default JwtInfoTable;
