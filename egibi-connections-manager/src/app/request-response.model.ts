export class RequestResponse {
  responseData: any;
  responseCode: number;
  responseMessage: string;
  responseError: ResponseError;
}

export class ResponseError {
  errorCode: string;
  exceptionMessage: string;
  innerExceptionMessage: string;
}
