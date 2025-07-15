// Universal error handler for API responses
export const handleApiError = (error: any, defaultMessage: string = 'An error occurred'): string => {
    if (error instanceof Error) {
        return error.message;
    }

    if (typeof error === 'string') {
        return error;
    }

    if (error?.response?.data?.message) {
        return error.response.data.message;
    }

    if (error?.message) {
        return error.message;
    }

    return defaultMessage;
};

// Show error alert with proper message
export const showErrorAlert = (error: any, defaultMessage: string = 'An error occurred'): void => {
    const errorMessage = handleApiError(error, defaultMessage);
    alert(errorMessage);
};

// Show success alert
export const showSuccessAlert = (message: string): void => {
    alert(message);
};
