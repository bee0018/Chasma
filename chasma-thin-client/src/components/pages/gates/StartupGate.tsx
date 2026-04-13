import { useEffect, useState } from "react";
import { useCacheStore } from "../../../managers/CacheManager";
import { appConfigClient } from "../../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";

/**
 * Initializes a new instance of the StartupGate component.
 * @constructor
 */
function StartupGate() {
    /** Gets or sets a flag indicating whether the the request is being loaded. */
    const [loading, setLoading] = useState(true);

    /** Gets or sets a value indicating whether the system is configured. */
    const [isConfigured, setIsConfigured] = useState<boolean | null>(null);

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The navigate function. */
    const navigate = useNavigate();

    useEffect(() => {
        const getIsSystemReady = async () => {
            try {
                setNotification({
                    title: "Determining if the system configuration is complete...",
                    message: "Please wait while we evaluate.",
                    isError: false,
                    loading: true
                });

                const response = await appConfigClient.getSystemReady();
                setIsConfigured(response.isReady!);
            } catch {
                // fallback: assume not configured
                setIsConfigured(false);
            } finally {
                setLoading(false);
                setNotification(null);
            }
        };

        getIsSystemReady();
    }, [setNotification]);

    useEffect(() => {
        if (loading || isConfigured === null) return;

        if (!isConfigured) {
            navigate("/setup");
        } else {
            navigate("/login");
        }
    }, [loading, isConfigured, navigate]);

    return null;
}

export default StartupGate;