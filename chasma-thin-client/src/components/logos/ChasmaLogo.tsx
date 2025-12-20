// ChasmaLogo.jsx
import React from 'react';

const ChasmaLogo = ({ size = 96, circleFill = "#00bfff", textFill = "#ffffff" }) => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 48 48"
        width={size}
        height={size}
    >
        <circle cx="24" cy="24" r="20" fill={circleFill} />
        <text
            x="24"
            y="24"
            textAnchor="middle"
            dominantBaseline="middle"
            fontFamily="Segoe UI, Tahoma, Geneva, Verdana, sans-serif"
            fontSize="10"
            fill={textFill}
        >
            Chasma
        </text>
    </svg>
);

export default ChasmaLogo;
