
/**
 * Initializes a new instance of the Chasma Logo
 * @param size The default size of the icon.
 */
const ChasmaLogo = ({ size = 96 }) => (
   <img
        src="/favicon.svg"
        width={size}
        height={size}
        alt="Chasma Logo"
        style={{ borderRadius: "50%" }}
    />
);

export default ChasmaLogo;
