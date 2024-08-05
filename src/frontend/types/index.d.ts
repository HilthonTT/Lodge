type UserAuth = {
  id: string;
  email: string;
  name: string;
  imageId?: string;
};

type LoginRequest = {
  email: string;
  password: string;
};

type RegisterRequest = {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
};
